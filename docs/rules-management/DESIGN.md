# Rules Management — Design

Status: **draft** (2026-11-23). Captures the decisions from the first design discussion. Nothing here is built yet.

## 1. Goal

Let people author, test, version and publish **decision rules** for an application without a rebuild of that
application. Rules run on the existing `Pondhawk.Rules` engine. The management system lives in its own repository and
consumes the `pondhawk-rules` packages from NuGet. The pieces an app needs at runtime — dynamic loading and unloading,
the DSL compiler, the restriction layer and the rule bundle format — are added to `pondhawk-rules` as general features
(§12). The management system hands apps a signed **rule bundle** of source; the app compiles it (§9).

Two authoring paths, one format underneath:

- **Developers and power users** write a rule in a restricted C# fluent DSL, in a Monaco editor with Roslyn-backed
  IntelliSense.
- **Business users** fill in a wizard generated from a template. The template renders the same C# DSL.

## 2. Scope: decision rules, not validation rules

| | Validation rules | Decision rules |
|---|---|---|
| Purpose | Guard entity invariants | Make decisions, set values, classify, route |
| Written in | C#, in the application | The rules DSL, in the management system |
| Lifecycle | Built and shipped with the app | Published at runtime |
| Runs | At `SaveChanges`, via `RuleValidationInterceptor` | When app code calls the named rule set (§14, Q3) |

Rules for the split:

- Validation rules stay in code. The management system does not manage them.
- Decision rules and validation rules are **separate rule sets**. Do not load them into one `RuleSet`: validation rules
  run at very high salience (`ValidationRule` uses `int.MaxValue - 1000000`), so they could fire before a decision rule
  changes the entity.
- Order of execution: decision rule set → `SaveChanges` → `RuleValidationInterceptor`. An entity that a decision rule
  makes invalid is rejected by the existing validation path. No new code is needed for this.
- Validation catches an **invalid** entity, not a **wrong but valid** one. Testing before publish (§10) covers that.

## 3. The DSL: restricted C# fluent builder

The DSL is C#, compiled at runtime with Roslyn. A rule is a call chain on a builder:

```csharp
rule.When(o => o.Customer.Tier == Tier.Gold)
    .And(o => o.Total > 1000m)
    .Set(o => o.DiscountRate, 0.10m)
    .Raise("Pricing", "Gold discount applied to {0}", o => o.Number);
```

Why C#:

- No parser and no expression-tree builder to write. Roslyn produces the delegates the engine already takes.
- Real type checking against the real entity types. Compiler errors appear in the editor as markers.
- One language for validation rules and decision rules.
- Plain text source: easy to store, diff, version and review.

### 3.1 One rule per document

The unit of management is **one rule per document**. Enable, disable, versioning and attribution all work per rule.
The published rule set compiles all its documents as **one compilation**.

### 3.2 Header and body

A document has two parts:

- **Header** — filled in through a form, never typed: rule name, fact types (1 to 4), salience, mutex, fire-once,
  inception, expiration.
- **Body** — the C# the user writes.

The fact types fix the generic signature. The compiler wraps the body in a generated template, hidden from the editor:

```csharp
// generated
static void Define(SafeRule<Order, Customer> rule)
{
    // document body
}
```

The editor shows only the body. The language server adds the template offset when it maps completions, hover and
diagnostics.

### 3.3 `SafeRule` builder

A scripting-only builder, `SafeRule<T1..T4>`, wraps the core `Rule<T1..T4>`. Its design rules:

- **It takes `Expression<Func<...>>`, never `Func<...>`.** It has **no `Action` consequences.**
- **All writes go through `Set(member, value)`.** The builder checks the member is writable in the catalog (§5).
- Consequences are a small, fixed set: `Set`, `Raise` (event with template and arguments), `Affirm` and `Veto`.
  `Insert` (new fact) is not in v1 (§14, Q4). Arbitrary method calls are not available.
- Expressions are compiled with `Compile()` and handed to the core `Rule<...>` as delegates. This needs no engine change.

What expression trees give us:

- **The C# compiler enforces part of the safety.** An expression-tree lambda can't contain a statement body, an
  assignment, a loop, `async` or `dynamic`. These are compile errors in the document.
- **Rules can be inspected before they run.** Walk each expression tree to:
  - check every member against the catalog (one place for the allowlist check);
  - record the members each rule **reads** and **writes** (used for attribution, conflict detection and drift, §6);
  - show a readable summary of the rule in the management UI.

## 4. Restriction layer

.NET has no sandbox: no CAS, no AppDomains. Static analysis before compile is the only guard. Security depends on it
being correct.

The restriction layer lives in `Pondhawk.Rules.Scripting` and runs **in both places**: in the management system (editor,
tests, publish) and in the app at every bundle load (§9.3). The app never trusts that the management system checked.

1. **Syntax whitelist.** Allow lambdas, member access, operators, literals and object creation of allowed types. Reject
   loops, `goto`, `unsafe`, `async`, local functions, `typeof`, and anything else not listed.
2. **Semantic check.** Every symbol the document uses must be in the catalog (entities, members, navigations) or on the
   allowed function list (`Math`, `string`, `DateTime`, `decimal`, LINQ over collection navigations). This blocks
   `System.IO`, `Process`, `HttpClient`, reflection and the rest.
3. **Expression walk** (§3.3) checks again after compile, against the catalog.

With no loops and no recursion, a rule can't hang. This matters: a running delegate can't be stopped safely.

The restriction layer needs **escape tests**: reflection through `GetType()`, `dynamic`, delegates hidden in allowed
types, extension methods brought in by `using`, and similar.

## 5. Catalog

The catalog is a **data model**: a policy over the CLR types, not a copy of them. It decides what rules may see and
change. Rules still compile against the **real CLR types**; the catalog only restricts and describes them. (Rules
written against catalog names mapped to entities were considered and rejected: that brings back a translation
language.)

### 5.1 Model

- **Entity** — CLR type name, display name, description, selectable as a fact (yes/no).
- **Member** — name, CLR type, exposure (none / read / read-write), nullability, enum values, description.
- **Navigation** — target entity, cardinality, followable (yes/no).
- **Function** — allowed helper methods.
- **Operation context** — read-only values from Pondhawk.Context's `Operation` (tenant, subject), exposed as a
  read-only fact or as allowed functions, so a rule can say "applies to tenant X". The engine does not know about
  Pondhawk.Context; the catalog and `SafeRule` expose it.
- **Catalog version** — each published rule set pins the catalog version it was compiled against.

Descriptions feed Monaco hover and completion docs.

### 5.2 Snapshot = facts, catalog = policy

- The **snapshot** (§6) records what exists in the app's model. It holds no exposure decisions.
- The **catalog** records what is exposed. It never invents members.
- Importing a snapshot produces a **diff** against the curated catalog.

Defaults: every entity and member starts **not exposed**. A person decides what rules may read and write. Keys,
concurrency tokens and audit columns are never writable.

## 6. Introspector and CI snapshot

### 6.1 Snapshot tool

- A `dotnet` tool (or MSBuild target) loads the app's build output and gets the EF Core `IModel` through the app's
  `IDesignTimeDbContextFactory` — the same pattern EF Core migrations tooling uses. No database connection is needed.
  Reflection is the fallback for types outside a `DbContext`.
- `IModel` supplies entity types, properties, navigations with cardinality, keys, owned types, enums, nullability and
  max length.
- Output is a JSON file:
  - snapshot format version;
  - app identity, app version, commit SHA;
  - entities, members, navigations, enums, nullability.
- Output is deterministic (sorted, stable formatting) so diffs are readable.

### 6.2 In the app's repo and CI

- **Commit the snapshot to the app repo**, as EF Core does with its model snapshot. Entity changes show in pull
  requests.
- CI regenerates the snapshot and **fails if the committed copy is out of date**.
- The CI artifact (or the app's package) carries the snapshot to the management system for import.
- **Optional gate:** CI fetches the currently published decision rule sets and compiles them against the new build. It
  fails the build if a rule breaks. This needs CI read access to the rule store or an exported rule bundle.

### 6.3 Drift

| Change in the entities | Result on import |
|---|---|
| New member | Listed as not exposed. Nothing breaks. |
| Removed or renamed member | Marked broken. The recorded read/write sets list exactly which rules break. |
| Type change | Rules recompiled and checked. Errors shown in the management UI. |

### 6.4 Runtime check

Each published rule set records the snapshot hash it was compiled against. At startup the app compares that hash with
its own model. On mismatch it recompiles the rule set, skips any rule that fails, and raises an alert (§14, Q1).

## 7. Templates and wizard

Business users author rules through a wizard. A template renders the C# DSL. The C# is still the one format that is
stored, compiled, versioned and attributed.

### 7.1 Template engine: Fluid

Templates use [Fluid](https://github.com/sebastienros/fluid) (Liquid for .NET). Fluid is built for templates that are
not trusted: no code runs, and member access is whitelisted through `TemplateOptions.MemberAccessStrategy`.

### 7.2 Template file

One file: YAML front matter (the **manifest**) followed by the Fluid **body**. Fluid can't infer parameter types, so the
manifest declares the model.

```yaml
---
id: threshold-discount
version: 3
facts: [ { name: order, kind: entity } ]
parameters:
  - { name: amount,   kind: member, of: order, type: decimal, access: read,  label: "Amount field" }
  - { name: minimum,  kind: decimal, min: 0,                                  label: "Minimum amount" }
  - { name: target,   kind: member, of: order, type: decimal, access: write, label: "Set this field" }
  - { name: rate,     kind: percent,                                          label: "Discount" }
---
rule.When(o => o.{{ amount }} > {{ minimum }})
    .Set(o => o.{{ target }}, {{ rate }});
```

The manifest fills the header fields that the template fixes (fact types) and the wizard collects the rest.

### 7.3 Safe rendering

Fluid outputs text, so injection is a risk. Make the safe path the default:

- **The model holds typed values, not raw strings.** Each value is a `CSharpLiteral` (decimal, string, date, enum) or a
  `CSharpMember` (a member path checked against the catalog).
- **Fluid `ValueConverters`** make a plain `{{ x }}` write an escaped C# literal (`0.10m`, a quoted and escaped string, a
  qualified enum member). No filter is needed and none can be forgotten.
- **No raw text output**, or only a `raw` filter limited to identifier-safe values.
- **Check after rendering.** The output goes through the Roslyn parse, the restriction layer and the compiler, exactly
  like a hand-written document. A template adds no trust.

### 7.4 Dynamic wizard

The wizard is built from the manifest. Each parameter `kind` maps to a Telerik component:

| kind | Component |
|---|---|
| `entity` | DropDownList of selectable catalog entities |
| `member` | DropDownList of catalog members, filtered by `type` and `access` |
| `decimal`, `int`, `percent` | NumericTextBox, min/max from the manifest |
| `date` | DatePicker |
| `enum` | DropDownList of catalog enum values |
| `rows` | Grid; one row per generated rule |

Steps:

1. Pick a template.
2. Bind the facts.
3. Fill in the parameters.
4. **Preview** — the rendered C# in a read-only Monaco editor, with compiler and restriction-layer diagnostics live.
5. Save.

Manifest constraints (required, min/max, relations such as "lower band < upper band") are validation rules. Check them
with a `Pondhawk.Rules` validation rule set over the wizard model.

### 7.5 Rule groups

With one rule per document and no `||` (see the authoring guidelines in `CLAUDE.md`), a table such as tiered bands
produces **one rule per row**. For a `rows` parameter, Fluid renders the body once per row, with the row values and the
shared values. The generated documents form a **rule group**: managed as one unit, sharing a mutex.

### 7.6 Link to the template

A generated document stores:

- template id and version;
- parameter values (JSON);
- the rendered C#.

Behavior:

- The C# is **read-only**. The rule reopens in the wizard.
- A new template version can **re-render** every rule that uses it, from the stored values, with a diff for review.
- Editing the C# by hand **detaches** the rule. It becomes a normal document and does not go back.

Templates are developer-authored and versioned. Each template has tests that render it with sample values and compile
the output against a test catalog.

## 8. Editor (Monaco)

Monaco's built-in C# support colors syntax but does not know the app's types. A Roslyn language service runs on the
server:

- `AdhocWorkspace` holding the generated template plus the document body, referencing the app's entity assembly.
- `CompletionService`, quick info (hover) and diagnostics, wired to Monaco's completion, hover and marker providers.
- Completions are **filtered by the catalog**: unexposed members do not appear.
- Restriction-layer errors appear as markers, the same as compiler errors.

## 9. Rule bundle, compilation and loading

### 9.1 The handoff is source, not assemblies

The management system hands the app a **rule bundle** that contains **source**. The app compiles it.

Why:

- **The app needs the compiler anyway.** On a model mismatch the app recompiles against its own model (§14, Q1).
- **The same compiler and restriction layer on both sides.** The management system uses them for editor diagnostics,
  tests and publish checks; the app uses them at load. "Compiles at publish" means "compiles at runtime", given the
  same model.
- **Defense in depth.** The app runs the restriction layer itself. It never runs a binary it can't check.
- **One path.** No separate "load assembly" and "recompile on mismatch" paths.

### 9.2 Rule bundle

The bundle format is the **contract** between the management system and the app. It is defined in
`Pondhawk.Rules.Scripting` (§12). A bundle contains:

- rule-set name and version;
- the rule documents: header and C# body. Template rules are already rendered — apps never see templates or Fluid;
- the **catalog allowlist** the bundle was approved against: the entities and members rules may read and write, and the
  allowed functions. Descriptions, display names and other curation data stay in the management system;
- the snapshot hash the bundle was compiled against;
- a **signature** over all of the above.

The signature matters because the allowlist travels in the bundle: a tampered bundle could widen what rules may touch.
The app verifies the signature on every load, including a load from its local cache.

### 9.3 Load path in the app

1. Fetch the bundle from the management service (§14, Q2), or read the local cache.
2. Verify the signature.
3. Run the restriction layer (§4) against the bundle's allowlist and the app's real types.
4. Compile all documents as **one compilation** into one assembly. Skip and report any rule that fails (§14, Q1).
5. Call `DynamicRuleSet.Load` with the assembly bytes (§9.4). The new generation replaces the old one.
6. Cache the bundle and the compiled assembly locally, keyed by bundle hash plus model hash. A warm start skips the
   compile.

A cold compile takes from hundreds of milliseconds to a few seconds.

### 9.4 `DynamicRuleSet` (engine change)

A new rule set in `Pondhawk.Rules` core that loads and unloads compiled rule assemblies and releases their memory. It
has no Roslyn dependency: it takes assembly bytes. A normal `dotnet build` rules assembly deployed as a file works the
same way, so it is a general plugin feature, not management-specific code.

```csharp
var rules = new DynamicRuleSet();
rules.Load(assemblyStream, pdbStream);   // build a new generation, swap atomically
var result = rules.Evaluate(order);      // uses the generation current at call time
rules.Unload();                          // drop the current generation
rules.Unloaded += (s, e) => ...;         // generation collected, or leak warning
```

Behavior:

1. **One generation per load.** A load creates a collectible `AssemblyLoadContext`, loads the assembly, finds the
   `IBuilder` types with the existing `RuleBuilderSource.AddTypes(assembly)` and builds a new rule tree. ALC, assembly
   and rule tree form a **generation**.
2. **Atomic swap.** `Interlocked.Exchange` replaces the current generation. Loads are serialized.
3. **Pinned per evaluation.** An evaluation captures the current generation once at its start; a swap never mixes
   versions inside one evaluation. `AbstractEvaluator.Evaluate` already builds its `EvaluationPlan` from
   `GetRuleBase()` once per call.
4. **Unload when idle.** Each generation counts its in-flight evaluations. When an old generation reaches zero, call
   `AssemblyLoadContext.Unload()`. A `WeakReference` to the ALC confirms the memory was released; raise `Unloaded`, or
   a leak warning if the ALC is still alive after a timeout.

**Type identity is critical.** The ALC resolves `Pondhawk.Rules` and the host's entity assemblies from the **default**
context (its `Load` override returns `null`). Otherwise `Order` inside the ALC is a different type from the host's
`Order`, and no rule matches.

**Unloading is cooperative.** Any live reference to a type or delegate from the ALC keeps it in memory:

- `RuleThreadLocalStorage` is `[ThreadStatic]`. `AbstractEvaluator` saves the caller's context and restores it in
  `finally` (null for a top-level evaluation). It does not pin. `[ThreadStatic]` stays: evaluation is synchronous and
  the context is read on the hot path. `Activity` / `AsyncLocal` was considered and rejected — slower, silently a no-op
  without a current `Activity`, and it would keep the context (and its `IRule` keys) alive for the whole request.
- **Listeners** receive `IRule` in `FiringRule` and `FiredRule`. A listener that stores the rule pins the generation.
  Document this.
- A rule that writes an object of a type defined in the ALC onto a host fact pins the generation. The restriction layer
  prevents this for DSL rules; for plain plugin assemblies it is a documented rule.
- `RuleEvent` and `EvaluationResults` hold **no rule objects** (checked 2026-11-23). `RuleEvent` holds only strings;
  the message is formatted with `string.Format` at creation, so parameter objects are not kept. `FiredRules` and
  `MutexWinners` are keyed by rule name and mutex name.
- `EvaluationResults.Shared` is `IDictionary<string, object>`. A rule that stores an object of an ALC type there pins
  the generation for as long as the caller keeps the results. `SafeRule` gives no access to `Shared`; for plugin
  assemblies it is a documented rule.
- `EvaluationContext.FireOnceRules` is keyed by the `IRule` object. A caller that keeps an `EvaluationContext` after
  evaluation pins the generation. Fix in `DynamicRuleSet` work: clear `FireOnceRules` when evaluation ends, or key it by
  rule name.

**Test:** load, evaluate, unload, then loop `GC.Collect()` / `GC.WaitForPendingFinalizers()` until the `WeakReference`
clears; fail if it does not. This catches pins as the engine changes.

## 10. Lifecycle and testing

- States: draft → tested → approved → published; roll back to an earlier published version.
- Approval: one user with the Approver role who is not an author of any change in the version (§14, Q6).
- **Test before publish**: run the candidate rule set against sample or recorded facts. Show the facts before and after,
  the events and scores, and the rules that fired.
- A published rule set is immutable. A change is a new version.

## 11. Attribution

When a decision rule produces an entity that fails validation, the `EntityValidationException` names the validation
rule, not the decision rule that caused it. To trace it back:

- Record which decision rules fired, per fact, and which members they wrote (`IEvaluationListener.FiredRule`,
  `EvaluationResults.FiredRules`, plus the write sets from §3.3).
- Record the rule-set version that ran.
- Record the operation's **correlation ID** from Pondhawk.Context, so a validation failure links to the decision run
  that caused it.
- Make that record available next to the validation failure.

## 12. Repository and packaging

The rules management system is **not built in the `pondhawk-rules` repo**. It gets its own repository and references
the `pondhawk-rules` packages as **NuGet dependencies**, like any other consumer.

The split follows one question: **does the app need it at runtime?**

- **Yes → `pondhawk-rules`.** The app loads, checks and compiles rule bundles itself (§9). So the runtime pieces — the
  engine, `DynamicRuleSet`, `SafeRule`, the restriction layer, the compiler and the bundle format — live in
  `pondhawk-rules`.
- **No → management repo.** Templates render to source before publish, and curation, editing and lifecycle happen only
  in the management system.

Rules for `pondhawk-rules`:

- The core `Pondhawk.Rules` package takes no dependency on Roslyn, Fluid or Blazor. Apps that use only C# rules never
  pull in Roslyn.
- Engine changes for the management system are made as **general features**, released as a new package version first,
  then taken up by the management repo through a version bump.

| Package | Repo | Contents | Heavy dependency |
|---|---|---|---|
| `Pondhawk.Rules` | `pondhawk-rules` | Engine; **`DynamicRuleSet`** (new, §9.4) | — |
| `Pondhawk.Rules.EFCore` | `pondhawk-rules` | Validation interceptor (unchanged) | EF Core |
| `Pondhawk.Rules.Scripting` | `pondhawk-rules` (new) | `SafeRule`, restriction layer, Roslyn compiler, **rule bundle format**, signature check, bundle loader and cache | `Microsoft.CodeAnalysis.CSharp` |
| Catalog and snapshot tool | Management repo | Full catalog model (curation data), snapshot format, `dotnet` snapshot tool used by app CI | EF Core (tool only) |
| Templates | Management repo | Manifest model, Fluid rendering, safe value converters | Fluid |
| Management application | Management repo | Editor and language service, wizard, catalog curation, lifecycle, rule store, bundle signing and publishing | Blazor, Telerik, Monaco |

The Monaco language service uses `Pondhawk.Rules.Scripting` too, so the editor and the app report the same errors.

### 12.1 Engine change list

Changes needed in `pondhawk-rules`, found so far:

1. **`DynamicRuleSet`** in `Pondhawk.Rules` — load and unload rule assemblies through collectible
   `AssemblyLoadContext` (§9.4).
2. **`Pondhawk.Rules.Scripting`** — new package (§3.3, §4, §9.2, §9.3).
3. Possibly: attribution hooks (§11), if `IEvaluationListener` and `EvaluationResults.FiredRules` are not enough.

The management application is a line-of-business app. By the standing database decision it starts on **local Postgres
+ pgBackRest**.

## 13. Limits

- **Security rests on the restriction layer.** There is no runtime sandbox.
- **C# is not friendly to non-developers.** The wizard is the answer for business users; free-form documents stay a
  developer and power-user path.
- **Validation catches invalid, not wrong.** Testing before publish is required.
- **Compile cost.** Cold compiles take seconds and happen in the app; cache the compiled assembly.
- **Apps that use DSL rules carry Roslyn.** `Pondhawk.Rules.Scripting` adds `Microsoft.CodeAnalysis.CSharp` to the app.
- **Unloading is cooperative.** A stray reference to a rule keeps its generation in memory (§9.4).
- **Rules are only as current as the catalog.** Drift is caught at snapshot import, in CI (optional gate) and at
  startup — not before.

## 14. Decisions (2026-11-23)

- **Q1 — Snapshot mismatch at startup: start without the failing rules.** The app recompiles the rule set against its
  own model, skips each rule that fails, logs it and raises an alert. The app runs; the skipped decisions are not made
  until the rules are fixed and published again.
- **Q2 — Delivery: the app pulls from the management service.** The app fetches published rule bundles (§9.2) at startup and
  when the service notifies a change. It keeps the **last good copy** in a local cache, so an unreachable service does
  not block startup. If the service is unreachable and no cache exists, the app starts with no decision rules and
  raises an alert (consistent with Q1).
- **Q3 — Invocation: explicit, in application code.** The app calls a named decision rule set at the point where the
  decision is needed (for example, pricing an order). No interceptor runs decision rules on save.
- **Q4 — No `Insert` in v1.** v1 consequences are `Set`, `Raise`, `Affirm` and `Veto`. `Insert` can come later, limited
  to catalog entities marked insertable.
- **Q5 — First templates:** threshold, eligibility, tiered bands, classification / routing. A date window needs no
  template: any rule gets one through the header's inception and expiration.
- **Q6 — Approval: one approver who is not an author.** A user with the Approver role approves a version. That user
  must not be the author of any change in the version.

## 15. Open questions

- Change notification for Q2: push (SignalR, webhook) or polling with a short interval?
- Local cache location for Q2. (Tampering is covered: the app verifies the bundle signature on every load, §9.2.)
- Bundle signing: key type, where the management system keeps the private key, how apps get and rotate the public key.
- Confirm the Q5 templates against real decision rules from an app.
