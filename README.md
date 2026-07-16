<p align="center">
  <img src="pht-small-logo.png" alt="Pondhawk.Rules" width="120" />
</p>

<h1 align="center">Pondhawk.Rules</h1>

<p align="center">
  A forward-chaining rule engine with type-based fact matching, validation, and weighted decisions — plus an EF Core SaveChanges validation adapter.
</p>

<p align="center">
  <a href="https://github.com/pondhawktech/pondhawk-rules/actions/workflows/build.yml"><img src="https://github.com/pondhawktech/pondhawk-rules/actions/workflows/build.yml/badge.svg" alt="Build" /></a>
  <img src="https://img.shields.io/badge/.NET-8.0-512bd4" alt=".NET 8" />
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License" />
  <a href="https://www.nuget.org/packages/Pondhawk.Rules"><img src="https://img.shields.io/nuget/v/Pondhawk.Rules?label=Rules" alt="Pondhawk.Rules on NuGet" /></a>
  <a href="https://www.nuget.org/packages/Pondhawk.Rules.EFCore"><img src="https://img.shields.io/nuget/v/Pondhawk.Rules.EFCore?label=Rules.EFCore" alt="Pondhawk.Rules.EFCore on NuGet" /></a>
</p>

Two packages, both `net8.0` and fully standalone (no dependency on other Pondhawk packages):

| Package | Description |
|---------|-------------|
| [**Pondhawk.Rules**](src/Pondhawk.Rules/README.md) | Forward-chaining rule engine — fluent rule definition, multi-fact matching (up to 4 fact types), validation, weighted decision scoring, forward chaining |
| [**Pondhawk.Rules.EFCore**](src/Pondhawk.Rules.EFCore/README.md) | EF Core `SaveChangesInterceptor` that validates `Added`/`Modified` entities through a rule set before they hit the database |

## Installation

```bash
dotnet add package Pondhawk.Rules
dotnet add package Pondhawk.Rules.EFCore   # only if you need the EF Core adapter
```

## Pondhawk.Rules

### Define and Evaluate Rules

```csharp
using Pondhawk.Rules.Factory;

var ruleSet = new RuleSet();

ruleSet.AddRule<Person>("age-check")
    .When(p => p.Age >= 18)
    .Then(p => p.Status = "Adult");

ruleSet.AddRule<Person>("can-vote")
    .When(p => p.Age >= 18)
    .And(p => p.IsActive)
    .Then(p => p.CanVote = true);

var result = ruleSet.Evaluate(new Person { Name = "Alice", Age = 25 });
// result.TotalFired, result.Events, result.Score
```

### Multi-Fact Rules & Pattern Matching

Rules match across up to 4 fact types. Per-fact `When()`/`And()` overloads take a single-fact predicate, enabling C# pattern matching with full type inference:

```csharp
ruleSet.AddRule<Person, Order>("gold-discount")
    .When((Func<Person, bool>)(p => p.Tier is "Gold" or "Platinum"))
    .And((Func<Order, bool>)(o => o.Total is > 200 and < 10_000))
    .Then((p, o) => o.DiscountPct = 0.10m);
```

### Validation

```csharp
ruleSet.AddValidation<Person>("name-required")
    .Assert(p => p.Name).Required();

ruleSet.AddValidation<Person>("age-range")
    .Assert(p => p.Age)
    .IsGreaterThanOrEqual(0)
    .IsLessThanOrEqual(150);

bool valid = ruleSet.TryValidate(person, out var violations);
```

Built-in validators cover `string` (`Required`, `IsEmail`, `MinLength`, `Matches`, …), numerics (`IsGreaterThan`, `IsBetween`, `IsPositive`, …), `DateTime` (`IsInFuture`, `IsWeekday`, …), `bool`, `Enum`, and `IEnumerable<T>`. See the [package README](src/Pondhawk.Rules/README.md#validation-extensions) for the full table.

### Scoring / Decisions

```csharp
ruleSet.AddRule<Application>("credit-check")
    .When(a => a.CreditScore > 700)
    .FireAffirm(10);

ruleSet.AddRule<Application>("bankruptcy")
    .When(a => a.HasBankruptcy)
    .FireVeto(20);

bool approved = ruleSet.Decide(application); // Score >= 0
```

### Class-Based Builders & DI

```csharp
public class OrderRules : RuleBuilder<Order>
{
    public OrderRules()
    {
        Rule().When(o => o.Total > 1000).Then(o => o.RequiresApproval = true);
        Rule().When(o => o.Items.Count == 0).Then(o => o.Status = "Empty");
    }
}

services.UseRules(typeof(OrderRules).Assembly);
```

### Authoring Guidelines

- **Avoid `||` in conditions** — write one rule per OR branch. Rules evaluate independently, so this keeps them atomic and individually traceable in `EvaluationResults`.
- **Prefer `.And()` chains over `&&`** — each condition gets its own predicate, improving readability and traceability.

Full concept reference (salience, mutex, fire-once, forward chaining, inception/expiration) is in the [package README](src/Pondhawk.Rules/README.md).

## Pondhawk.Rules.EFCore

Validate entities automatically on `SaveChanges`:

```csharp
using Pondhawk.Rules.EFCore;
using Pondhawk.Rules.Factory;

var ruleSet = new RuleSet();
ruleSet.AddValidation<Order>("total-positive")
    .Assert<decimal>(o => o.Total).IsGreaterThan(0m);

var options = new DbContextOptionsBuilder<AppContext>()
    .UseSqlServer(connectionString)
    .AddRuleValidation(ruleSet)
    .Options;

// On SaveChanges, all Added/Modified entities are validated through the rule
// set; EntityValidationException is thrown (with structured violations) before
// the save reaches the database.
```

`RuleValidationInterceptor : SaveChangesInterceptor` requires **EF Core 5.0+** (the release that introduced SaveChanges interception). See the [package README](src/Pondhawk.Rules.EFCore/README.md) for details.

## Repository Layout

```
src/Pondhawk.Rules/         The rule engine (net8.0)
src/Pondhawk.Rules.EFCore/  EF Core SaveChanges adapter (net8.0)
test/Pondhawk.Rules.Tests/  xUnit tests
build/                      Cake (Frosting) build script
.github/workflows/          build.yml (build/test/pack → GitHub Packages) + publish.yml (→ NuGet.org)
```

## Building

```bash
# Restore, build, and test via the Cake script
dotnet run --project build/Build.csproj -- --target=Test

# Or use the SDK directly
dotnet build pondhawk-rules.slnx
dotnet test pondhawk-rules.slnx
```

## Versioning & Packaging

Each package's version is `major.minor` from its `version.json` with a build number as the patch. Off-CI builds get a `-local` prerelease suffix.

```bash
dotnet run --project build/Build.csproj -- --target=Pack --build-number=<n>   # writes ./artifacts/*.nupkg
```

## CI/CD

- **`build.yml`** — on push/PR to `main`: build + test; on `main` it also packs and pushes both packages to the `pondhawktech` GitHub Packages feed and uploads the `.nupkg`s as an artifact.
- **`publish.yml`** — manual `workflow_dispatch` that promotes a build's artifacts to **NuGet.org** (uses the org-level `NUGET_ORG_API_KEY`).

## History

Extracted from the [pondhawktech/tools](https://github.com/pondhawktech/tools) monorepo. `Pondhawk.Rules` was consumed there by `Pondhawk.Api` (which stays in `tools` and now references it as a NuGet package). During extraction, `Pondhawk.Rules.EFCore` was lowered from `net10.0` to `net8.0` — nothing in it required net10.

## License

MIT — see [LICENSE](LICENSE).
