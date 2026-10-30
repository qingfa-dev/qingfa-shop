# Monadic Error Handling — Design and Refactor Policy

- **Status:** Approved
- **Date:** 2026-10-30
- **Applies to:** Core, Application, Infrastructure, Api, Gateway
- **Supersedes:** ad-hoc throw/Result choices in earlier slices

## 1. Why

The repository already uses `Result` / `Result<T>` and `Error`, but 142 `throw` sites
remain. Most are legitimate boundaries (DI guards, startup composition, EF/JSON/Wolverine
adapters, cancellation). A minority sit in business or parsing flow and force callers to
catch exceptions for expected outcomes. This policy makes the distinction explicit and
moves the flow-level sites to `Result`, so exceptions stay exceptional.

## 2. Rules

### 2.1 Result-first

A method returns `Result` / `Result<T>` when a failure is an expected outcome:

- parsing or normalizing input;
- validation of a command/query/value object;
- graph building (dependency cycles, unknown dependencies, duplicate registrations);
- aggregate replay/rehydration;
- external adapter failures that the caller can retry, report, or compensate.

Every such failure uses a stable `*Outcome` factory. Do not return raw strings or booleans
when a caller needs a reason.

### 2.2 Validator-first

Input shape rules live in FluentValidation validators when the input is a request,
options object, or message. Validators return `ValidationResult`; callers convert failures
to `Error` via the existing validation behavior or an outcome factory. Constructors do not
carry multi-rule business validation.

### 2.3 Try-first, throwing wrapper last

For a public API that historically threw, provide a `TryXxx` method returning `Result` and
keep the old method as a thin boundary wrapper:

```csharp
public static PermissionMetadata Parse(string identifier)
{
    Result<PermissionMetadata> result = TryParse(identifier);
    // Boundary: callers that still use the throwing API get one documented exception.
    return result.IsSuccess ? result.Value : throw new FormatException(result.Errors[0].Message);
}
```

Internal code must call `TryParse`, not `Parse`.

### 2.4 Allowed throws

A throw is allowed only in these categories, and each site carries a
`// Boundary: <reason>` comment:

| Category | Examples | Why |
|---|---|---|
| DI/constructor guards | `ArgumentNullException.ThrowIfNull` in constructors | Programming error; no recoverable runtime outcome |
| Startup composition | invalid provider, missing RabbitMQ connection, migration failure | Fail fast before serving traffic |
| Adapter boundary | JSON converters/serializers, EF expression translation, RabbitMQ | Third-party contracts require exceptions; adapters translate to `Result` |
| Cancellation | `OperationCanceledException` | Must preserve cancellation semantics |
| Explicit final unwrap | `Result.ThrowIfFailure()` in Wolverine/MediatR boundaries | The transport needs an exception to retry/dead-letter |
| Throwing convenience wrapper | `Parse`, `Create`, `Build`, `WithName` | Transitional API over a `TryXxx`; documented boundary |

Anything else is a policy violation.

### 2.5 Outcome quality

- One outcome catalog per feature/concern (`*.Outcome.cs`), with stable dotted codes.
- Each factory names the exact failing concern and includes actionable context.
- Codes are constants; messages are built by factories, not scattered strings.
- New failure paths must add a factory and a test that asserts the code.

### 2.6 Validation quality

- FluentValidation validators own request/options rule sets.
- Validators share constants (`*.Constant.cs`) instead of magic numbers.
- A `TryXxx` factory that must run without a DI validator uses the validator's rules
  directly or a small dedicated validator instance; it must not re-implement them.
- Add boundary cases: null, empty, whitespace, max length, duplicate, malformed,
  unsupported mode, and cancellation.

## 3. Refactor scope in this pass

1. `Metadata`: add `TryGetValue<T>` / `TryGetRequiredValue<T>` returning `Result<T>`; keep
   the throwing accessors as documented boundary wrappers.
2. `SearchRequest`: add `TryCreate` returning `Result<SearchRequest>`; use it from
   `SearchText.Transformer` and `Query.Builder`. Constructors become boundary wrappers.
3. `PermissionPolicyParser`: add `TryCompose` returning `Result<string>`; the attribute
   constructor uses it and throws at the attribute boundary with a comment.
4. `InitializationPipelineBuilder`: add `TryBuild` returning `Result<IInitializationPipeline>`;
   `EfCorePersistenceInitializer.InitializeAsync` uses it; `Build` remains a boundary
   wrapper for `GetPipeline`.
5. `EventSourcedAggregate`: add `TryLoadFromHistory` returning `Result` with
   `EventSourcingOutcome`; `LoadFromHistory` remains a boundary wrapper for the event-store
   adapter.
6. Add `// Boundary:` comments to the remaining throws in composition, converters,
   interceptors, and transport adapters.
7. Tests: `Try` success/failure, error codes, validator edge cases, and one test that the
   throwing wrapper preserves the `Try` outcome.

## 4. Out of scope

- Removing DI constructor guards.
- Replacing exceptions inside `System.Text.Json`, EF Core, or RabbitMQ.
- Changing cancellation behavior.
- Rewriting the whole 142-site inventory in one pass; the remaining sites are listed as
  boundary exceptions and reviewed incrementally.

## 5. Acceptance

- New flow-level code returns `Result`; no new business throw.
- Every remaining throw has a boundary comment or is a constructor/DI guard.
- New `Try` APIs have unit tests asserting both the value and the error code.
- `make ci` stays green; coverage floors hold.
- `docs/specs/2026-10-29-event-driven-architecture-design.md` links this policy.
