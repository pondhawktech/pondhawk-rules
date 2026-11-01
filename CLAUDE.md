# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Restore, build, and test via the Cake build script
dotnet run --project build/Build.csproj -- --target=Test

# Build the solution directly
dotnet build pondhawk-rules.slnx

# Build a single project
dotnet build src/Pondhawk.Rules/Pondhawk.Rules.csproj
dotnet build src/Pondhawk.Rules.EFCore/Pondhawk.Rules.EFCore.csproj

# Run tests directly
dotnet test pondhawk-rules.slnx

# Pack NuGet packages (writes to ./artifacts)
dotnet run --project build/Build.csproj -- --target=Pack
```

## Project Setup

- **.NET 8** — both `Pondhawk.Rules` and `Pondhawk.Rules.EFCore` target `net8.0` (`LangVersion=default`, `Nullable=disable`).
- **Central package management** via `Directory.Packages.props`.
- `TreatWarningsAsErrors` on; Meziantou analyzer enforced (`src/Directory.Build.props`).
- Versioning: `<VersionPrefix>` in `src/Directory.Build.props` holds the last released version, and `publish.yml` bumps it. Pushes to `main` publish `-ci.<n>` prereleases of the next patch; a local `Pack` gets `-local`.

## Architecture

Two packages: the standalone rule engine and an EF Core adapter that hooks it into `SaveChanges`.

### Pondhawk.Rules — Rule Engine (standalone, no other Pondhawk dependency)

A forward-chaining rule engine with type-based fact matching. Uses `Microsoft.Extensions.Logging` for listener infrastructure (Serilog picks up MS Logging events transparently). Key subsystems:

- **Builder** (`Pondhawk.Rules.Builder`): Fluent API for defining rules. `RuleBuilder<TFact1..TFact4>` creates `Rule<T>` instances via `When().And().Then()` chains. Supports up to 4 fact types per rule. Multi-fact rules have per-fact `When(Func<T, bool>)`/`And(Func<T, bool>)` overloads that enable C# pattern matching with full type inference. Rules have salience (priority), mutex (mutual exclusion), fire-once, inception/expiration.
- **Evaluation** (`Pondhawk.Rules.Evaluation`): `EvaluationPlan` generates all fact-type combinations using variations-with-repetition. `TupleEvaluator` executes rules in salience order against fact tuples. `FactSpace` stores facts with int selectors for memory efficiency.
- **Tree** (`Pondhawk.Rules.Tree`): `RuleTree` indexes rules by fact types for fast lookup with polymorphic type matching.
- **Validation** (`Pondhawk.Rules.Validators`): `ValidationBuilder<TFact>` with `Assert<T>(expr).Is().IsNot().Otherwise()` chains. Runs at very high salience.
- **Factory** (`Pondhawk.Rules.Factory`): `RuleSet` for runtime rule creation without predefined builder classes. `RuleSetFactory` uses `Lazy<T>` for thread-safe exactly-once initialization.
- **Listeners** (`Pondhawk.Rules.Listeners`): Observer pattern (`IEvaluationListener`) for tracing rule evaluation. `WatchEvaluationListener` uses `Microsoft.Extensions.Logging.ILogger`. `WatchEvaluationListenerFactory` uses `ILoggerFactory` (defaults to `NullLoggerFactory`).
- **RuleEvent**: Sealed event type with `IEquatable<RuleEvent>`, init-only setters, nested `EventCategory` enum (`Info`, `Warning`, `Violation`).
- **Exceptions**: `ViolationsExistException`, `NoRulesEvaluatedException`, `EvaluationExhaustedException` — all extend `Exception` directly.

Evaluation flow: `RuleBuilder` → `RuleTree` (indexed by type) → `EvaluationPlan` (generates steps) → `TupleEvaluator` (executes) → `EvaluationResults` (aggregates scores/events/violations). Forward chaining via `InsertFact`/`ModifyFact`/`RetractFact` triggers re-evaluation.

**Authoring guidelines**: Avoid `||` in conditions — each OR branch should be a separate rule for atomicity and traceability. Prefer `.And()` chains over `&&` inside a single predicate.

### Pondhawk.Rules.EFCore — EF Core SaveChanges Validation

Pre-save entity validation interceptor that hooks into EF Core's `SaveChangesInterceptor`. Validates all `Added` and `Modified` entities through `Pondhawk.Rules` before they reach the database.

- **RuleValidationInterceptor**: `SaveChangesInterceptor` subclass that pulls entities from `ChangeTracker`, calls `IRuleSet.Validate()`, and throws `EntityValidationException` if validation fails. Overrides both `SavingChanges` and `SavingChangesAsync`.
- **EntityValidationException**: Carries `ValidationResult` with structured violations. Formats a human-readable message from violations.
- **DbContextOptionsBuilderExtensions**: `AddRuleValidation(IRuleSet)` convenience method.
- References `Pondhawk.Rules` (project reference within this repo). Minimum EF Core version: 5.0.0 (the release that introduced `SaveChangesInterceptor`). Targets `net8.0` — EF Core 5.0 (netstandard2.1) imposes no higher floor.

## Conventions

- Namespaces match folder structure: `Pondhawk.Rules`, `Pondhawk.Rules.Builder`, `Pondhawk.Rules.Evaluation`, `Pondhawk.Rules.Tree`, `Pondhawk.Rules.Validators`, `Pondhawk.Rules.Factory`, `Pondhawk.Rules.Listeners`, `Pondhawk.Rules.EFCore`.

## History

Extracted from the [pondhawktech/tools](https://github.com/pondhawktech/pondhawk-utilities) monorepo (now `pondhawk-utilities`), where these lived as `src/Pondhawk.Rules` and `src/Pondhawk.Rules.EFCore`. `Pondhawk.Rules` was consumed there by `Pondhawk.Api` (now retired; its last version is at the tag `api-final` in `pondhawk-utilities`). During extraction `Pondhawk.Rules.EFCore` was lowered from `net10.0` to `net8.0` — nothing in it required net10.

## CI/CD

- `.github/workflows/build.yml` — builds, tests, packs, and pushes both packages to the `pondhawktech` GitHub Packages feed on pushes to `main`; uploads `.nupkg` artifacts.
- `.github/workflows/publish.yml` — `workflow_dispatch` that bumps the version and releases to **NuGet.org** (uses nuget.org Trusted Publishing; no secret).
