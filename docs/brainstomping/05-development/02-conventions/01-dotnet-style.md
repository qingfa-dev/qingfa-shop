# .NET and C# Style

The C# language rules for resys-eshop: nullable reference types, file-scoped namespaces, records for contracts, asynchronous and cancellation conventions, analyzers, and the formatting command.

**Links:** [Module conventions](./02-module-conventions.md) · [Naming](./04-naming.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Module contracts](../../03-architecture/02-components/02-module-contracts.md) · [Contributing](../../00-governance/02-policies/01-contributing.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the language-level style contract for every `.cs` file under `src/` and `tests/`. It
fixes the compiler settings that ship in `Directory.Build.props`, the shapes allowed for domain
types and DTOs, the rules for `async`/`await` and cancellation, and the analyzer and formatting
gates a pull request must pass. It deliberately excludes project structure, which is
`05/02-02`, and identifier spelling, which is `05/02-04`.

The guiding rule: **the compiler and analyzers enforce style wherever possible, so review can spend
its attention on behavior.** Anything expressible as a build error or a `.editorconfig` rule is not
left to reviewer discretion.

---

## Compiler Settings (`Directory.Build.props`)

Every project inherits one set of settings from the repository root. A project does not override
these individually.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

- **`Nullable` is `enable` everywhere.** Nullable reference types are not optional; an oblivious
  project is a defect. `#nullable disable` appears only in generated code, never in hand-written
  source.
- **`TreatWarningsAsErrors` is on.** A new warning fails the build locally and in CI (`05/03-01`,
  `05/03-03`). When a warning is genuinely wrong for a line, suppress it narrowly with a
  `#pragma warning disable` and a one-line reason, never repo-wide.
- **`ImplicitUsings` is on** for the common `System.*` and ASP.NET namespaces. A type outside that
  set gets an explicit `using`.
- **Package versions are centralized.** `Directory.Packages.props` holds one version per package;
  a `.csproj` uses `<PackageReference Include="..." />` with no `Version` attribute.

---

## Namespaces and File Layout

- **File-scoped namespaces only.** A file declares `namespace Resys.Modules.Catalog.Domain;` and
  the type bodies follow unindented. Block-scoped namespaces are not used in hand-written code.
- **One top-level type per file**, and the file name matches the type name
  (`Order.cs` contains `Order`). Small related types may share a file only when they are nested,
  private, or a closed set of one-line records.
- **`using` directives are outside the namespace** (the C# default), alphabetized, with
  `System.*` first. Unused usings are removed before commit; the analyzer flags them.
- **Nullable flow is explicit.** Parameters, return types, and fields are non-nullable unless
  absence is meaningful, in which case the type is `T?` and the call site handles `null`. The
  null-forgiving operator `!` is a last resort that requires an immediate, obvious reason (for
  example a test asserting a known non-null result).

---

## Domain Types vs. Contracts

| Kind | Shape | Placement | Mutable? |
|---|---|---|---|
| Entity | `sealed class` with private setters and behavior methods | module `Domain/` | Yes, through methods only |
| Value object (`Money`, typed ids) | `readonly record struct` or `sealed record` | `Resys.Core` | No |
| Domain event | `sealed record` with a past-tense name | `Resys.Core.Events` | No |
| Cross-module contract | `interface` with async methods | `Resys.Core.Contracts.<Module>` | n/a |
| HTTP DTO | `sealed record` | `Resys.Shared.<Area>` | No |
| Options | `sealed class` or `sealed record` bound from configuration | owning project | Bound once at start |

- **Records for DTOs.** Every request and response type in `Resys.Shared` is a `sealed record`
  with positional or init-only properties. Value equality and immutability are the point; DTOs
  carry no behavior.
- **Entities are classes, not records.** An entity has identity and a lifecycle; its setters are
  private and its state changes only through methods that keep its invariants. A record
  `with`-expression must never clone an entity.
- **Value objects are records.** `Money`, `VariantId`, `StoreId`, and similar are immutable and
  compare by value. `Money` is the only representation of an amount; a raw `decimal` amount does
  not cross a public boundary (`03/03-03`).
- **No public setters on aggregate state.** An order's status is mutated by the state machine
  (`03/02-03`), not by property assignment.

---

## Async and Await

- **Suffix every asynchronous method with `Async`** (`FindAsync`, `PlaceOrderAsync`), including
  interface members. A method that returns `Task` but is named without the suffix is a defect.
- **Return `Task`/`Task<T>`; use `ValueTask<T>` only on a hot path with a measured allocation
  reason.** A method that completes synchronously may return the cached `Task`; it still carries
  the `Async` suffix.
- **`async void` is forbidden** except for a top-level event handler, which must catch and log its
  own exceptions.
- **Await, never block.** `.Result`, `.Wait()`, and `Task.Run(() => io()).Result` are banned
  inside the service. Blocking a request thread is a correctness and throughput defect.
- **Pass the token through.** Every layer forwards its `CancellationToken` to the next call and to
  EF Core (`ToListAsync(ct)`, `SaveChangesAsync(ct)`).
- **`ConfigureAwait(false)` is not used** in ASP.NET Core application code; the host has no
  synchronization context, so the call adds noise without benefit.
- **Stream with `IAsyncEnumerable<T>`** for large reads and forward the token; do not buffer a
  whole result set only to return it as a list unless the consumer needs random access.

```csharp
public sealed class OrderQuery(OrdersDbContext db) : IOrderQuery
{
    public Task<OrderSummary?> FindAsync(OrderId id, CancellationToken ct = default)
        => db.Orders
            .Where(order => order.Id == id)
            .Select(order => OrderSummary.From(order))
            .FirstOrDefaultAsync(ct);
}
```

---

## Cancellation Tokens

- **Last parameter, named `ct`, defaulted.** The signature is
  `Task<T> DoAsync(..., CancellationToken ct = default)`. A public async method without a token is
  a defect unless it is a pure, allocation-free computation.
- **The HTTP boundary supplies the token.** Minimal API handlers take a `CancellationToken ct`
  parameter bound by the framework (the request-aborted token) and pass it to the service call.
- **No new token sources** (`CancellationTokenSource`) except to impose a real timeout on an
  outbound call, and then the source is disposed. A timeout deadline is configuration, not a
  literal in the method.
- **Background and outbox work** receives the token owned by the host's `IHostApplicationLifetime`
  so shutdown cancels in-flight work; a background loop never uses `CancellationToken.None`.
- **Swallowing cancellation is forbidden.** `OperationCanceledException` propagates; it is not
  caught and converted into a failure result.

---

## Analyzers and Formatting

- **The built-in .NET analyzers** (`AnalysisLevel=latest-recommended`) run on every build with
  `EnforceCodeStyleInBuild=true`; their warnings are errors. This is the primary automated style
  gate and needs no extra package.
- **`.editorconfig` is the style source of truth** for spacing, `using` ordering, expression
  bodies, and modern-syntax preferences (`var` policy, target-typed `new`, pattern matching). The
  file lives at the repository root and is committed.
- **Formatting is `dotnet format`.** Format before committing and verify in CI:

  ```bash
  # apply formatting + analyzer fixes locally
  dotnet format Resys.sln

  # verify without writing (CI mode); non-zero exit means unformatted code
  dotnet format Resys.sln --verify-no-changes --severity warn
  ```

- **Nullable and analyzer suppressions are local.** Prefer fixing the code; a suppression is one
  line, justified in a comment, and never a project-wide `<NoWarn>`.
- **Do not hand-format around the tool.** If `dotnet format` disagrees with a preference, change
  `.editorconfig` in its own commit so the tool and the team agree.

---

## Logging and Errors

- **Structured logging only.** Log with message templates (`logger.LogInformation("Order {OrderId} placed", id)`),
  never string interpolation; a PII or PAN value never enters a log, a metric, or an exception
  message (`03/04-03`).
- **Throw domain exceptions, return problem details.** The domain throws specific exception types;
  the API boundary maps them to RFC 9457 problem details. A service does not construct HTTP
  responses.
- **No exceptions for control flow.** A missing entity is a `null` result from a query contract; a
  validation failure is a typed result, not a thrown exception.
- **Catch narrowly.** A `catch (Exception)` exists only at a documented boundary that logs and
  rethrows or converts; the default is to let the framework handle it.

---

## Testing Style

- **xUnit with `[Fact]`/`[Theory]`**, one behavior per test, Arrange–Act–Assert, test name
  `Method_State_Expected`.
- **Integration tests use Testcontainers PostgreSQL** (`06/01-03`); a test never depends on a
  developer's local database.
- **Async tests are `async Task`**, never `async void`; they accept and forward a
  `CancellationToken` when the framework provides one.
- **No `Thread.Sleep`**; time is injected as `IClock` so tests control it.

## Open Questions

1. Whether a stricter third-party analyzer (`StyleCop.Analyzers` or a Roslynator set) is layered on
   top of the built-in analyzers is decided when the solution is scaffolded in `05/03-01`; the
   built-in set plus `.editorconfig` is the floor regardless.
2. Whether `GenerateDocumentationFile` is enabled for `Resys.Shared` (the client-facing contract)
   is confirmed with the OpenAPI client generation in `03/04-04`; it stays off until then.
3. The exact `var` preference is fixed in `.editorconfig` during scaffolding; this file does not
   restate a rule the tool already enforces.

## Links

- [Module conventions](./02-module-conventions.md)
- [Naming](./04-naming.md)
- [Solution layout](../../03-architecture/02-components/01-solution-layout.md)
- [Module contracts](../../03-architecture/02-components/02-module-contracts.md)
- [Contributing](../../00-governance/02-policies/01-contributing.md)
- [Design spec](../../11-spec/README.md) — §4 (stack),
  §5 (solution layout).
- Build and run: `05/03-01`; CI gates: `05/03-03`; module rules: `05/02-02`.
