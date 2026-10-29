# Slice ① — Domain Design Closure

- **Status:** Approved
- **Date:** 2026-10-29
- **Depends on:** none
- **Unblocks:** slices ② and ④

## 1. Goal

Close the DDD building-block gaps that the layer READMEs already advertise, without
changing the Todo API or introducing infrastructure dependencies into `Core`.

Deliverables:

1. Specification pattern.
2. Domain policy pattern.
3. Stable event identity and version on every domain event.
4. Enforced optimistic concurrency for every aggregate.
5. Event-sourced aggregate base and aggregate factory contract.

## 2. Non-goals

- No event store, outbox, inbox or transport in this slice.
- No `IClock` abstraction: handlers already use the .NET `TimeProvider`, which is the
  native clock port and is already injected by `CompleteTodo` and friends.
- No marker-only `IDomainService`: it would add ceremony without behavior.
- No shop domain.

## 3. Specifications

### Location

`src/Core/Common/Domain/Specifications`

### Contracts

`Specification.Interface.cs`

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T candidate);
}
```

`Specification.Base.cs`

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T candidate)
        => ToExpression().Compile()(candidate);

    public Specification<T> And(ISpecification<T> other);
    public Specification<T> Or(ISpecification<T> other);
    public Specification<T> Not();
}
```

- `And`/`Or`/`Not` return `Specification<T>` so chaining keeps working on the base class;
  an `ISpecification<T>`-typed reference cannot chain unless it is a `Specification<T>`.
- `And`/`Or`/`Not` return composite specifications that preserve expression trees.
- Composites are internal nested types; no public surface beyond the interface.
- `IsSatisfiedBy` compiles once per call. It is for in-memory rules, not query paths.

`Specification.Extension.cs`

```csharp
public static IQueryable<T> Where<T>(
    this IQueryable<T> query,
    ISpecification<T> specification);

public static bool Any<T>(
    this IQueryable<T> query,
    ISpecification<T> specification);
```

### Rules

- `Core` must not reference EF Core. Only `System.Linq.Expressions` is used.
- Null specification arguments fail with `ArgumentNullException`; no silent no-op.
- A composite must not flatten or evaluate the expression; EF must be able to translate it.

### Tests

`tests/Unit/Core.Unit.Tests/Common/Domain/Specifications/`

- a simple specification matches and rejects.
- `And` requires both, `Or` requires either, `Not` negates.
- composite expression still translates through `IQueryable` with a fake provider or
  in-memory list.
- null arguments throw.

## 4. Domain policies

### Location

`src/Core/Common/Domain/Policies`

### Contracts

`Policy.Interface.cs`

```csharp
public interface IDomainPolicy<in TContext, TResult>
{
    Result<TResult> Evaluate(TContext context);
}
```

`Policy.Base.cs`

```csharp
public abstract class DomainPolicy<TContext, TResult>
    : IDomainPolicy<TContext, TResult>
{
    public abstract Result<TResult> Evaluate(TContext context);
}
```

### Purpose

A policy is a named, testable decision that spans more than one entity or concerns a rule
that does not belong to a single aggregate. The first consumer is slice ④’s reminder
scheduling rule (a reminder cannot be scheduled for a completed todo).

### Rules

- Policies are pure: no I/O, no repository, no clock. Inputs are passed in the context.
- A policy returns a `Result<TResult>`; it never throws for an expected business outcome.
- Policies are registered in DI by their concrete type; no assembly scan is required.

### Tests

`tests/Unit/Core.Unit.Tests/Common/Domain/Policies/`

- successful evaluation returns the value.
- rejected evaluation returns a failure with a stable code.
- null context behavior is explicit (throw for programming error, or a documented failure).

## 5. Event identity and versioning

### Location

`src/Core/Common/Domain/Events`

### Change

`DomainEvent.Interface.cs` becomes:

```csharp
public interface IDomainEvent : INotification, IHasMetadata
{
    string EventName { get; }
    int EventVersion { get; }
}
```

`DomainEvent.Base.cs` adds:

```csharp
public virtual string EventName => GetType().Name;
public virtual int EventVersion => 1;
```

### Rules

- `EventName` is the stable wire name. Renaming the CLR type without overriding
  `EventName` is a breaking change and must be treated as a new event version.
- `EventVersion` starts at `1`. A breaking payload change increments it and adds an
  upcaster in slice ④.
- Existing events (`TodoCreated`, `TodoCompleted`, …) need no edits; they inherit the
  defaults.
- Tests that build anonymous `IDomainEvent` implementations must implement the two
  members.

### Tests

`tests/Unit/Core.Unit.Tests/Common/Domain/Events/`

- a concrete event reports its CLR type name and version 1.
- an event can override the name and version.
- metadata, event id and timestamp behavior is unchanged.

## 6. Optimistic concurrency

### Problem

`IVersionable` and `VersionableInterceptor` exist, but no entity implements
`IVersionable` and `Version` is not an EF concurrency token. Concurrent updates can
silently overwrite each other.

### Design

1. `Aggregate<TKey, TEvent>` implements `IVersionable` and exposes:

   ```csharp
   public long Version { get; set; }
   ```

   Every aggregate therefore carries a version; `Todo` gets it without a manual edit.

2. Add `src/Infrastructure/Persistence/EntityFramework/Conventions/Versionable.Configuration.cs`:

   ```csharp
   public static class VersionableConfiguration
   {
       public static void Apply(ModelBuilder modelBuilder);
   }
   ```

   For every entity type assignable to `IVersionable`, mark the `Version` property as a
   concurrency token. Call it from `ModelBuilderConventionExtensions.ApplyDefaultConventions`
   after the enum convention.

3. `VersionableInterceptor` keeps incrementing the version on modified entries. EF now
   sends the original version in the `WHERE` clause.

4. Add a migration `AddAggregateVersion`:
   - `Todos.Version bigint NOT NULL DEFAULT 0`
   - `TodoLists.Version bigint NOT NULL DEFAULT 0`
   - `TodoCategories.Version bigint NOT NULL DEFAULT 0`
   - no other table changes.

5. `EfCoreUnitOfWork` already translates `DbUpdateConcurrencyException` to
   `UnitOfWorkOutcome.Failure.ConcurrencyConflict`; add a test proving the path.

### Rules

- A new aggregate persists at version 0. The first successful update becomes 1. The token
  protects updates; an insert does not need it.
- A stale update must fail, never overwrite.
- API models do not expose `Version` unless a use case explicitly opts in.

> Row `Version` (the optimistic token guarded by the `WHERE` clause) and
> `IEventSourcedAggregate.Version` (the stream position) are different concepts. They can
> hold different values for the same aggregate and must not be treated as interchangeable.

### Tests

- `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Conventions/Versionable.Configuration.Test.cs`
  asserts `IsConcurrencyToken()` on a versioned test entity and skips non-versioned types.
- `tests/Integration/Infrastructure.Integration.Tests/Persistence/Concurrency.Test.cs`
  loads the same Todo twice, saves both, and asserts the second save returns a
  concurrency failure against PostgreSQL.

## 7. Event-sourced aggregate base

### Location

`src/Core/Common/Domain/EventSourcing`

### Contracts

`EventSourcedAggregate.Interface.cs`

```csharp
public interface IEventSourcedAggregate<out TKey>
{
    TKey Id { get; }
    long Version { get; }
    IReadOnlyList<IDomainEvent> UncommittedEvents { get; }
    void LoadFromHistory(long version, IEnumerable<IDomainEvent> history);
    void ClearUncommittedEvents();
}
```

`EventSourcedAggregate.Base.cs`

```csharp
public abstract class EventSourcedAggregate<TKey> : IEventSourcedAggregate<TKey>
{
    public TKey Id { get; protected set; } = default!;
    public long Version { get; private set; }
    public IReadOnlyList<IDomainEvent> UncommittedEvents { get; }

    protected void Raise(IDomainEvent domainEvent);
    protected abstract void When(IDomainEvent domainEvent);
}
```

Semantics:

- `Version` is the number of events applied; an empty stream has version 0.
- `Raise` applies the event through `When`, increments `Version`, and appends to
  `UncommittedEvents` in that order.
- `LoadFromHistory(version, history)` applies events through `When` without adding them
  to `UncommittedEvents`, then sets `Version` to the supplied stream version.
- `When` must set `Id` when replaying the creation event. An unknown event type throws
  `InvalidOperationException`; silently ignoring events corrupts state.
- `ClearUncommittedEvents` is called by the repository only after a successful append.

`AggregateFactory.Interface.cs`

```csharp
public interface IAggregateFactory<out TAggregate>
{
    TAggregate Create();
}
```

Factories are registered per aggregate and let an infrastructure repository construct an
empty aggregate without reflection or a public constructor.

### Tests

`tests/Unit/Core.Unit.Tests/Common/Domain/EventSourcing/`

- raising events increments version and exposes them as uncommitted.
- replay applies events, sets the supplied version, and leaves uncommitted empty.
- clearing removes only uncommitted events.
- an unknown event type throws.
- a factory returns a fresh aggregate.

## 8. Composition impact

- No new DI registrations in slice ① except the migration.
- `Aggregate.Version` is mapped by convention, so no per-entity configuration edits.
- `DomainEventInterceptor` keeps working; it reads `IAggregate<IDomainEvent>`, which is
  unaffected by the new `Version` property.

## 9. Acceptance criteria

- `make lint` passes, including structure and format checks.
- `make lib.test` passes: Core, Application, Infrastructure, Architecture.
- `make coverage.unit` stays at or above 90% line / 80% branch for every gated assembly.
- `make api.test` passes unchanged.
- PostgreSQL concurrency integration test fails a stale update.
- No `Infrastructure` or EF type appears in `Core`.
