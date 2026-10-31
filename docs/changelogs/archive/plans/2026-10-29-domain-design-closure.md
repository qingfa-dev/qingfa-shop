# Domain Design Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add specifications, domain policies, versioned domain events, enforced aggregate concurrency, and an event-sourced aggregate base without changing the public Todo API.

**Architecture:** All new building blocks live in `Core` and depend only on `System.Linq.Expressions` and existing kernel types. Concurrency is enforced by an EF Core convention in `Infrastructure` plus a migration adding `Version` to the three aggregate tables. The event-sourced base is a pure in-memory state machine that slice ④ will drive from the event store.

**Tech Stack:** .NET 10, C# records, `System.Linq.Expressions`, EF Core 10, PostgreSQL 16, xUnit v3, AwesomeAssertions, Testcontainers.

**Spec:** `docs/specs/2026-10-29-domain-design-closure-design.md`

> **Error-handling update (2026-10-30):** all new and changed code follows
> `docs/specs/2026-10-30-monadic-error-handling-design.md`. Flow failures return `Result`;
> throws are boundary-only and carry a `// Boundary:` reason.

## Global Constraints

- Target framework `net10.0`; nullable enabled; analyzers on; warnings are errors.
- `Core` has zero project references and must not reference EF Core, Wolverine or RabbitMQ.
- `Application` references `Core` only.
- Every new file follows `guide/dotted-file-naming-guide.md`; the final dotted segment must be a known role.
- Comments use labeled intent (`Validate:`, `Enforce:`, `Raise:`), never freeform narration.
- Central Package Management: never add a `Version` attribute to a `PackageReference`.
- Tests use `dotnet test --project …`; MTP exits `8` when a filter matches zero tests.
- Coverage floors: 90% line / 80% branch per gated assembly.
- Stage only files named by the current task. The working tree contains unrelated external edits; never use `git add -A`.
- Commit after every task.

---

### Task 1: Specification Pattern

**Files:**
- Create: `src/Core/Common/Domain/Specifications/Specification.Interface.cs`
- Create: `src/Core/Common/Domain/Specifications/Specification.Base.cs`
- Create: `src/Core/Common/Domain/Specifications/Specification.Extension.cs`
- Test: `tests/Unit/Core.Unit.Tests/Common/Domain/Specifications/Specification.Test.cs`

**Interfaces:**
- Produces: `ISpecification<T>.ToExpression()`, `ISpecification<T>.IsSatisfiedBy(T)`, `Specification<T>.And/Or/Not`, `SpecificationExtensions.Where/Any`.

- [ ] **Step 1: Write the failing test**

Create `tests/Unit/Core.Unit.Tests/Common/Domain/Specifications/Specification.Test.cs`:

```csharp
using System.Linq.Expressions;

using Core.Common.Domain.Specifications;

namespace Core.Unit.Tests.Common.Domain.Specifications;

/// <summary>Tests for composable specifications.</summary>
[Trait("Category", "Unit")]
public sealed class SpecificationTest
{
    private sealed record Item(int Value, string Name);

    private sealed class GreaterThan(int threshold) : Specification<Item>
    {
        public override Expression<Func<Item, bool>> ToExpression()
            => item => item.Value > threshold;
    }

    private sealed class NameStartsWith(string prefix) : Specification<Item>
    {
        public override Expression<Func<Item, bool>> ToExpression()
            => item => item.Name.StartsWith(prefix);
    }

    [Fact]
    public void IsSatisfiedBy_WhenCandidateMatches_ReturnsTrue()
    {
        // Arrange
        var specification = new GreaterThan(5);

        // Act
        bool result = specification.IsSatisfiedBy(new Item(6, "alpha"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WhenCandidateDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var specification = new GreaterThan(5);

        // Act
        bool result = specification.IsSatisfiedBy(new Item(5, "alpha"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_RequiresBothSpecifications()
    {
        // Arrange
        var specification = new GreaterThan(5).And(new NameStartsWith("a"));
        var items = new[]
        {
            new Item(6, "alpha"),
            new Item(6, "beta"),
            new Item(4, "alpha"),
        }.AsQueryable();

        // Act
        var result = specification.IsSatisfiedBy(new Item(6, "alpha"))
            && !specification.IsSatisfiedBy(new Item(6, "beta"))
            && !specification.IsSatisfiedBy(new Item(4, "alpha"));

        // Assert
        result.Should().BeTrue();
        items.Where(specification).Should().ContainSingle()
            .Which.Should().Be(new Item(6, "alpha"));
    }

    [Fact]
    public void Or_RequiresEitherSpecification()
    {
        // Arrange
        var specification = new GreaterThan(5).Or(new NameStartsWith("a"));
        var items = new[]
        {
            new Item(6, "beta"),
            new Item(4, "alpha"),
            new Item(4, "beta"),
        }.AsQueryable();

        // Act
        var result = items.Where(specification).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new Item(6, "beta"));
        result.Should().Contain(new Item(4, "alpha"));
    }

    [Fact]
    public void Not_NegatesTheSpecification()
    {
        // Arrange
        var specification = new GreaterThan(5).Not();

        // Act
        bool matches = specification.IsSatisfiedBy(new Item(4, "alpha"));

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Any_WhenMatchingItemExists_ReturnsTrue()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(9, "b") }.AsQueryable();

        // Act
        bool result = items.Any(new GreaterThan(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_WithNullSpecification_Throws()
    {
        // Arrange
        var specification = new GreaterThan(1);

        // Act
        Action act = () => specification.And(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Composition_CanBeChained()
    {
        // Arrange
        var specification = new GreaterThan(5)
            .And(new NameStartsWith("a"))
            .Or(new NameStartsWith("b"))
            .Not();

        // Act
        bool matches = specification.IsSatisfiedBy(new Item(6, "beta"));

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Any_WhenNothingMatches_ReturnsFalse()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(2, "b") }.AsQueryable();

        // Act
        bool result = items.Any(new GreaterThan(5));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_ThroughQueryable_FiltersMatches()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(9, "b") }.AsQueryable();

        // Act
        var result = items.Where(new GreaterThan(5).Not()).ToList();

        // Assert
        result.Should().ContainSingle().Which.Should().Be(new Item(1, "a"));
    }

    [Fact]
    public void Where_WithNullQuery_Throws()
    {
        // Arrange
        IQueryable<Item> query = null!;

        // Act
        Action act = () => query.Where(new GreaterThan(1));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Where_WithNullSpecification_Throws()
    {
        // Arrange
        var query = Array.Empty<Item>().AsQueryable();

        // Act
        Action act = () => query.Where((ISpecification<Item>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_WithNullSpecification_Throws()
    {
        // Arrange
        var query = Array.Empty<Item>().AsQueryable();

        // Act
        Action act = () => query.Any((ISpecification<Item>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `Core.Common.Domain.Specifications`.

- [ ] **Step 3: Write the contract**

Create `src/Core/Common/Domain/Specifications/Specification.Interface.cs`:

```csharp
using System.Linq.Expressions;

namespace Core.Common.Domain.Specifications;

/// <summary>Composable predicate over a candidate type.</summary>
/// <typeparam name="T">The candidate type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Returns the predicate as a translatable expression tree.</summary>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>Evaluates the predicate in memory.</summary>
    bool IsSatisfiedBy(T candidate);
}
```

- [ ] **Step 4: Write the base and composites**

Create `src/Core/Common/Domain/Specifications/Specification.Base.cs`:

```csharp
using System.Linq.Expressions;

namespace Core.Common.Domain.Specifications;

/// <summary>Base class for composable specifications.</summary>
/// <typeparam name="T">The candidate type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <inheritdoc />
    public bool IsSatisfiedBy(T candidate) => ToExpression().Compile()(candidate);

    /// <summary>Combines this specification with another using logical AND.</summary>
    public Specification<T> And(ISpecification<T> other)
        => new AndSpecification<T>(this, Require(other));

    /// <summary>Combines this specification with another using logical OR.</summary>
    public Specification<T> Or(ISpecification<T> other)
        => new OrSpecification<T>(this, Require(other));

    /// <summary>Negates this specification.</summary>
    public Specification<T> Not() => new NotSpecification<T>(this);

    private static ISpecification<T> Require(ISpecification<T> specification)
        => specification ?? throw new ArgumentNullException(nameof(specification));

    private static Expression<Func<T, bool>> Combine(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, Expression> merge)
    {
        // Merge: inline both bodies on one parameter so EF Core can translate the result.
        var parameter = Expression.Parameter(typeof(T), "candidate");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            merge(leftBody!, rightBody!),
            parameter);
    }

    private sealed class AndSpecification<TValue>(
        ISpecification<TValue> left,
        ISpecification<TValue> right) : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
            => Combine(left.ToExpression(), right.ToExpression(), Expression.AndAlso);
    }

    private sealed class OrSpecification<TValue>(
        ISpecification<TValue> left,
        ISpecification<TValue> right) : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
            => Combine(left.ToExpression(), right.ToExpression(), Expression.OrElse);
    }

    private sealed class NotSpecification<TValue>(ISpecification<TValue> inner)
        : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
        {
            Expression<Func<TValue, bool>> expression = inner.ToExpression();

            return Expression.Lambda<Func<TValue, bool>>(
                Expression.Not(expression.Body),
                expression.Parameters);
        }
    }

    private sealed class ParameterReplacer(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == source ? target : base.VisitParameter(node);
    }
}
```

Create `src/Core/Common/Domain/Specifications/Specification.Extension.cs`:

```csharp
namespace Core.Common.Domain.Specifications;

/// <summary>Queryable helpers for specifications.</summary>
public static class SpecificationExtensions
{
    /// <summary>Filters a query with a specification.</summary>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        return query.Where(specification.ToExpression());
    }

    /// <summary>Returns true when any row matches the specification.</summary>
    public static bool Any<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        return query.Any(specification.ToExpression());
    }
}
```

- [ ] **Step 5: Run the test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "Core.Unit.Tests.Common.Domain.Specifications.SpecificationTest"
```

Expected: PASS, 13 tests.

- [ ] **Step 6: Commit**

```bash
git add src/Core/Common/Domain/Specifications tests/Unit/Core.Unit.Tests/Common/Domain/Specifications
git commit -m "feat(core): add specification pattern"
```

---

### Task 2: Domain Policy Pattern

**Files:**
- Create: `src/Core/Common/Domain/Policies/Policy.Interface.cs`
- Create: `src/Core/Common/Domain/Policies/Policy.Base.cs`
- Test: `tests/Unit/Core.Unit.Tests/Common/Domain/Policies/DomainPolicy.Test.cs`

**Interfaces:**
- Consumes: `Result<T>`, `Error.Validation` from the existing kernel.
- Produces: `IDomainPolicy<TContext, TResult>.Evaluate(TContext)`, `DomainPolicy<TContext, TResult>`.

- [ ] **Step 1: Write the failing test**

Create `tests/Unit/Core.Unit.Tests/Common/Domain/Policies/DomainPolicy.Test.cs`:

```csharp
using Core.Common.Domain.Policies;

namespace Core.Unit.Tests.Common.Domain.Policies;

/// <summary>Tests for Result-returning domain policies.</summary>
[Trait("Category", "Unit")]
public sealed class DomainPolicyTest
{
    private sealed record OrderContext(int Quantity, int Stock);

    private sealed class StockPolicy : DomainPolicy<OrderContext, int>
    {
        public override Result<int> Evaluate(OrderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.Quantity > 0 && context.Quantity <= context.Stock
                ? Result<int>.Success(context.Quantity)
                : Result<int>.Fail(Error.Validation(
                    "Order.Quantity.Invalid",
                    "The quantity must be positive and available in stock."));
        }
    }

    [Fact]
    public void Evaluate_WhenRulePasses_ReturnsValue()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Result<int> result = policy.Evaluate(new OrderContext(2, 5));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public void Evaluate_WhenRuleFails_ReturnsStableFailure()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Result<int> result = policy.Evaluate(new OrderContext(6, 5));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Order.Quantity.Invalid");
    }

    [Fact]
    public void Evaluate_WithNullContext_Throws()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Action act = () => policy.Evaluate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `Core.Common.Domain.Policies`.

- [ ] **Step 3: Write the contract and base**

Create `src/Core/Common/Domain/Policies/Policy.Interface.cs`:

```csharp
namespace Core.Common.Domain.Policies;

/// <summary>Named business decision that spans entities or aggregates.</summary>
/// <typeparam name="TContext">The inputs the decision needs.</typeparam>
/// <typeparam name="TResult">The value produced on success.</typeparam>
public interface IDomainPolicy<in TContext, TResult>
{
    /// <summary>Evaluates the policy without side effects.</summary>
    Result<TResult> Evaluate(TContext context);
}
```

Create `src/Core/Common/Domain/Policies/Policy.Base.cs`:

```csharp
namespace Core.Common.Domain.Policies;

/// <summary>Base class for Result-returning domain policies.</summary>
public abstract class DomainPolicy<TContext, TResult>
    : IDomainPolicy<TContext, TResult>
{
    /// <inheritdoc />
    public abstract Result<TResult> Evaluate(TContext context);
}
```

- [ ] **Step 4: Run the test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "Core.Unit.Tests.Common.Domain.Policies.DomainPolicyTest"
```

Expected: PASS, 3 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Core/Common/Domain/Policies tests/Unit/Core.Unit.Tests/Common/Domain/Policies
git commit -m "feat(core): add domain policy pattern"
```

---

### Task 3: Versioned Domain Events

**Files:**
- Modify: `src/Core/Common/Domain/Events/DomainEvent.Interface.cs`
- Modify: `src/Core/Common/Domain/Events/DomainEvent.Base.cs`
- Test: `tests/Unit/Core.Unit.Tests/Common/Domain/Events/DomainEvent.Version.Test.cs`

**Interfaces:**
- Produces: `IDomainEvent.EventName`, `IDomainEvent.EventVersion`; `DomainEvent` defaults to the CLR type name and version `1`.

- [ ] **Step 1: Write the failing test**

Create `tests/Unit/Core.Unit.Tests/Common/Domain/Events/DomainEvent.Version.Test.cs`:

```csharp
using Core.Common.Domain.Events;

namespace Core.Unit.Tests.Common.Domain.Events;

/// <summary>Tests for stable event identity and versioning.</summary>
[Trait("Category", "Unit")]
public sealed class DomainEventVersionTest
{
    private sealed record PlainEvent : DomainEvent;

    private sealed record RenamedEvent : DomainEvent
    {
        public override string EventName => "order.placed";
        public override int EventVersion => 3;
    }

    [Fact]
    public void EventName_DefaultsToClrTypeName()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.EventName.Should().Be(nameof(PlainEvent));
    }

    [Fact]
    public void EventVersion_DefaultsToOne()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.EventVersion.Should().Be(1);
    }

    [Fact]
    public void EventName_CanBeOverriddenForWireCompatibility()
    {
        // Act
        var domainEvent = new RenamedEvent();

        // Assert
        domainEvent.EventName.Should().Be("order.placed");
        domainEvent.EventVersion.Should().Be(3);
    }

    [Fact]
    public void EventIdAndTimestamp_AreStillPopulated()
    {
        // Act
        var domainEvent = new PlainEvent();

        // Assert
        domainEvent.GetEventId().Should().NotBeNull();
        domainEvent.GetTimestamp().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS1061` for `EventName` / `EventVersion`.

- [ ] **Step 3: Extend the contract**

Replace `src/Core/Common/Domain/Events/DomainEvent.Interface.cs` with:

```csharp
using MediatR;

namespace Core.Common.Domain.Events;

/// <summary>
/// Represents a domain event with correlation and tenant context.
/// Extends <see cref="INotification"/> so domain events can be published via Mediator.
/// </summary>
public interface IDomainEvent : INotification, IHasMetadata
{
    /// <summary>Gets the stable wire name of the event.</summary>
    string EventName { get; }

    /// <summary>Gets the payload version of the event.</summary>
    int EventVersion { get; }
}
```

- [ ] **Step 4: Add the defaults**

Modify `src/Core/Common/Domain/Events/DomainEvent.Base.cs` so the record exposes:

```csharp
public virtual string EventName => GetType().Name;

public virtual int EventVersion => 1;
```

Place them in the `Properties` region before the constructor.

- [ ] **Step 5: Run the test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "Core.Unit.Tests.Common.Domain.Events.DomainEventVersionTest"
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Run the existing event tests**

Run:

```bash
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "*DomainEvent*"
```

Expected: PASS, all event tests.

- [ ] **Step 7: Commit**

```bash
git add src/Core/Common/Domain/Events/DomainEvent.Interface.cs src/Core/Common/Domain/Events/DomainEvent.Base.cs tests/Unit/Core.Unit.Tests/Common/Domain/Events/DomainEvent.Version.Test.cs
git commit -m "feat(core): version domain events"
```

---

### Task 4: Aggregate Version and Optimistic Concurrency

**Files:**
- Modify: `src/Core/Common/Domain/Aggregates/Aggregate.Interface.cs`
- Modify: `src/Core/Common/Domain/Aggregates/Aggregate.Base.cs`
- Create: `src/Infrastructure/Persistence/EntityFramework/Conventions/Versionable.Configuration.cs`
- Modify: `src/Infrastructure/Persistence/EntityFramework/Conventions/ModelBuilder.Convention.Extension.cs`
- Create (generated by Step 11): the `AddAggregateVersion` migration pair under `src/Infrastructure/Persistence/Migrations/`
- Test: `tests/Unit/Core.Unit.Tests/Common/Domain/Aggregates/Aggregate.Spec.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Conventions/Versionable.Configuration.Test.cs`
- Test: `tests/Integration/Infrastructure.Integration.Tests/Persistence/Concurrency.Test.cs`

**Interfaces:**
- Consumes: `IVersionable` from `Core.Common.Concerns.Foundation.Versionable`.
- Produces: `IAggregate` extends `IVersionable`; every aggregate has `long Version`; EF marks it as a concurrency token.

- [ ] **Step 1: Write the failing Core test**

Add to `tests/Unit/Core.Unit.Tests/Common/Domain/Aggregates/Aggregate.Spec.cs`:

```csharp
[Fact]
public void Version_ShouldStartAtZeroAndBeWritable()
{
    // Arrange
    var aggregate = new TestAggregate();

    // Act
    long initial = aggregate.Version;
    aggregate.Version = 4;

    // Assert
    initial.Should().Be(0);
    aggregate.Version.Should().Be(4);
}

[Fact]
public void Aggregate_ShouldImplementVersionable()
{
    // Arrange
    var aggregate = new TestAggregate();

    // Act
    IVersionable versionable = aggregate;

    // Assert
    versionable.Version.Should().Be(0);
}
```

Add `using Core.Common.Concerns.Foundation.Versionable;` to the file.

- [ ] **Step 2: Run the Core test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS1061` for `Version`.

- [ ] **Step 3: Add the version contract**

Replace `src/Core/Common/Domain/Aggregates/Aggregate.Interface.cs` with:

```csharp
using Core.Common.Concerns.Foundation.Versionable;
using Core.Common.Domain.Events;

namespace Core.Common.Domain.Aggregates;

/// <summary>
/// Defines a marker interface of an Aggregate as specified in Domain Driven Design.
/// </summary>
public interface IAggregate : IHasMetadata, IVersionable
{
}

/// <summary>
/// Defines an Event based contract of an Aggregate as specified in Domain Driven Design.
/// </summary>
/// <typeparam name="TEvent">The type of the event that implements the <see cref="IDomainEvent"/> interface.</typeparam>
/// <seealso cref="IAggregate" />
public interface IAggregate<out TEvent> : IAggregate
{
    /// <summary>
    /// Gets the events that was added to the Aggregate.
    /// </summary>
    IReadOnlyList<TEvent> Events { get; }

    /// <summary>
    /// Removes all events from the Aggregate.
    /// </summary>
    void RemoveAllEvents();
}
```

- [ ] **Step 4: Add the version property**

In `src/Core/Common/Domain/Aggregates/Aggregate.Base.cs`, add this property next to `Metadata`:

```csharp
/// <summary>
/// Gets or sets the optimistic concurrency version. New aggregates start at zero.
/// </summary>
public long Version { get; set; }
```

- [ ] **Step 5: Run the Core test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "Core.Unit.Tests.Common.Domain.Aggregates.AggregateSpec"
```

Expected: PASS.

- [ ] **Step 6: Write the failing convention test**

Create `tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Conventions/Versionable.Configuration.Test.cs`:

```csharp
using Core.Common.Concerns.Foundation.Versionable;

using Infrastructure.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Conventions;

/// <summary>Tests for the Version concurrency-token convention.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class VersionableConfigurationTest
{
    private sealed class VersionedEntity : IVersionable
    {
        public Guid Id { get; set; }
        public long Version { get; set; }
    }

    private sealed class UnversionedEntity
    {
        public Guid Id { get; set; }
        public long Version { get; set; }
    }

    [Fact]
    public void Apply_WhenEntityIsVersionable_MarksVersionAsConcurrencyToken()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<VersionedEntity>(builder =>
        {
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id).ValueGeneratedNever();
        });

        // Act
        VersionableConfiguration.Apply(modelBuilder);

        // Assert
        var property = modelBuilder.Model
            .FindEntityType(typeof(VersionedEntity))!
            .FindProperty(nameof(IVersionable.Version))!;
        property.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void Apply_WhenEntityIsNotVersionable_LeavesVersionAlone()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<UnversionedEntity>(builder =>
        {
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id).ValueGeneratedNever();
        });

        // Act
        VersionableConfiguration.Apply(modelBuilder);

        // Assert
        var property = modelBuilder.Model
            .FindEntityType(typeof(UnversionedEntity))!
            .FindProperty(nameof(UnversionedEntity.Version))!;
        property.IsConcurrencyToken.Should().BeFalse();
    }
}
```

- [ ] **Step 7: Run the convention test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `VersionableConfiguration`.

- [ ] **Step 8: Implement the convention**

Create `src/Infrastructure/Persistence/EntityFramework/Conventions/Versionable.Configuration.cs`:

```csharp
using Core.Common.Concerns.Foundation.Versionable;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Conventions;

/// <summary>
/// Marks the <see cref="IVersionable.Version"/> property as an EF Core concurrency token.
/// </summary>
public static class VersionableConfiguration
{
    /// <summary>Applies the convention to every versionable entity type.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IVersionable).IsAssignableFrom(entityType.ClrType))
                continue;

            var property = entityType.FindProperty(nameof(IVersionable.Version));
            if (property is null)
                continue;

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(IVersionable.Version))
                .IsConcurrencyToken();
        }
    }
}
```

- [ ] **Step 9: Register the convention**

In `src/Infrastructure/Persistence/EntityFramework/Conventions/ModelBuilder.Convention.Extension.cs`,
add the call inside `ApplyDefaultConventions` after `EnumAsIntConvention.Apply(...)`:

```csharp
VersionableConfiguration.Apply(modelBuilder);
```

- [ ] **Step 10: Run the convention test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build --filter-class "Infrastructure.Unit.Tests.Persistence.EntityFramework.Conventions.VersionableConfigurationTest"
```

Expected: PASS, 2 tests.

- [ ] **Step 11: Generate the migration**

Run:

```bash
dotnet ef migrations add AddAggregateVersion \
  -p src/Infrastructure \
  -s app/Web.Api \
  -o Persistence/Migrations
```

Inspect the generated migration. It must add only:

- `Todos.Version`
- `TodoLists.Version`
- `TodoCategories.Version`

each `bigint NOT NULL DEFAULT 0`. If unrelated operations appear, fix the model and
regenerate; do not commit a noisy migration.

- [ ] **Step 12: Write the failing concurrency integration test**

Create `tests/Integration/Infrastructure.Integration.Tests/Persistence/Concurrency.Test.cs`:

```csharp
using Application.Common.Persistence.UnitOfWorks;

using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Integration.Tests.Persistence.Todos;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Persistence;

/// <summary>Proves that a stale aggregate update is rejected by PostgreSQL.</summary>
[Collection("Todo EfCore")]
public sealed class ConcurrencyTest(TodoEfCoreFixture fixture) : IAsyncLifetime
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await fixture.CleanAsync();
        fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await fixture.CleanAsync();
    }

    [Fact]
    public async Task StaleUpdate_ReturnsConcurrencyConflict()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            tenantId,
            "concurrency",
            DateTimeOffset.UtcNow).Value;

        using var scope1 = fixture.Services.CreateScope();
        using var scope2 = fixture.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context1.Add(todo);
        await context1.SaveChangesAsync();
        context1.ChangeTracker.Clear();

        Todo first = await context1.Set<Todo>().SingleAsync(entity => entity.Id == todo.Id);
        Todo second = await context2.Set<Todo>().SingleAsync(entity => entity.Id == todo.Id);

        // Act: the first writer moves the row to version 1.
        first.Title = "first";
        first.Version = 1;
        await context1.SaveChangesAsync();

        // Act: the second writer still believes it is version 0.
        second.Title = "second";
        var unitOfWork = new EfCoreUnitOfWork(context2);
        Result result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.ConcurrencyConflict);
    }
}
```

- [ ] **Step 13: Run the integration test and verify it passes**

Run:

```bash
dotnet build tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "Infrastructure.Integration.Tests.Persistence.ConcurrencyTest"
```

Expected: PASS, 1 test. If Docker is unavailable the suite skips; start Docker before
claiming success.

- [ ] **Step 14: Commit**

```bash
git add \
  src/Core/Common/Domain/Aggregates/Aggregate.Interface.cs \
  src/Core/Common/Domain/Aggregates/Aggregate.Base.cs \
  src/Infrastructure/Persistence/EntityFramework/Conventions/Versionable.Configuration.cs \
  src/Infrastructure/Persistence/EntityFramework/Conventions/ModelBuilder.Convention.Extension.cs \
  src/Infrastructure/Persistence/Migrations \
  tests/Unit/Core.Unit.Tests/Common/Domain/Aggregates/Aggregate.Spec.cs \
  tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework/Conventions/Versionable.Configuration.Test.cs \
  tests/Integration/Infrastructure.Integration.Tests/Persistence/Concurrency.Test.cs
git commit -m "feat(core): enforce aggregate concurrency version"
```

---

### Task 5: Event-Sourced Aggregate Base

**Files:**
- Create: `src/Core/Common/Domain/EventSourcing/EventSourcedAggregate.Interface.cs`
- Create: `src/Core/Common/Domain/EventSourcing/EventSourcedAggregate.Base.cs`
- Create: `src/Core/Common/Domain/EventSourcing/AggregateFactory.Interface.cs`
- Test: `tests/Unit/Core.Unit.Tests/Common/Domain/EventSourcing/EventSourcedAggregate.Test.cs`

**Interfaces:**
- Consumes: `IDomainEvent`, `DomainEvent`.
- Produces: `IEventSourcedAggregate<TKey>` (`Id`, `Version`, `UncommittedEvents`, `LoadFromHistory`, `ClearUncommittedEvents`), `EventSourcedAggregate<TKey>.Raise/When`, `IAggregateFactory<TAggregate>.Create`.

- [ ] **Step 1: Write the failing test**

Create `tests/Unit/Core.Unit.Tests/Common/Domain/EventSourcing/EventSourcedAggregate.Test.cs`:

```csharp
using Core.Common.Domain.EventSourcing;
using Core.Common.Domain.Events;

namespace Core.Unit.Tests.Common.Domain.EventSourcing;

/// <summary>Tests for the event-sourced aggregate state machine.</summary>
[Trait("Category", "Unit")]
public sealed class EventSourcedAggregateTest
{
    private sealed record Created(Guid Id, string Name) : DomainEvent;

    private sealed record Renamed(string Name) : DomainEvent;

    private sealed record Unknown(string Value) : DomainEvent;

    private sealed class TestAggregate : EventSourcedAggregate<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        public static TestAggregate Create(Guid id, string name)
        {
            var aggregate = new TestAggregate();
            aggregate.Raise(new Created(id, name));
            return aggregate;
        }

        public void Rename(string name) => Raise(new Renamed(name));

        public void Break() => Raise(new Unknown("x"));

        protected override void When(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case Created created:
                    Id = created.Id;
                    Name = created.Name;
                    break;

                case Renamed renamed:
                    Name = renamed.Name;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Event '{domainEvent.GetType().Name}' is not supported.");
            }
        }
    }

    private sealed class TestAggregateFactory : IAggregateFactory<TestAggregate>
    {
        public TestAggregate Create() => new();
    }

    [Fact]
    public void Raise_AppliesEventAndIncrementsVersion()
    {
        // Act
        TestAggregate aggregate = TestAggregate.Create(Guid.NewGuid(), "first");

        // Assert
        aggregate.Version.Should().Be(1);
        aggregate.Name.Should().Be("first");
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<Created>();
    }

    [Fact]
    public void LoadFromHistory_AppliesEventsWithoutMarkingThemUncommitted()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        var history = new IDomainEvent[]
        {
            new Created(id, "first"),
            new Renamed("second"),
        };
        var aggregate = new TestAggregate();

        // Act
        aggregate.LoadFromHistory(2, history);

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.Name.Should().Be("second");
        aggregate.Version.Should().Be(2);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearUncommittedEvents_RemovesOnlyUncommittedEvents()
    {
        // Arrange
        TestAggregate aggregate = TestAggregate.Create(Guid.NewGuid(), "first");

        // Act
        aggregate.ClearUncommittedEvents();

        // Assert
        aggregate.UncommittedEvents.Should().BeEmpty();
        aggregate.Version.Should().Be(1);
        aggregate.Name.Should().Be("first");
    }

    [Fact]
    public void When_WithUnknownEvent_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(1, [new Unknown("x")]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }

    [Fact]
    public void Factory_CreatesAnEmptyAggregate()
    {
        // Arrange
        IAggregateFactory<TestAggregate> factory = new TestAggregateFactory();

        // Act
        TestAggregate aggregate = factory.Create();

        // Assert
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raise_WhenHandlerThrows_DoesNotAdvanceVersionOrBufferEvent()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.Break();

        // Assert
        act.Should().Throw<InvalidOperationException>();
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromHistory_WithEmptyHistory_KeepsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.LoadFromHistory(0, []);

        // Assert
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromHistory_WithNullHistory_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(0, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromHistory_WithNegativeVersion_Throws()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        Action act = () => aggregate.LoadFromHistory(-1, []);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
```

Expected: FAIL with `CS0246` for `Core.Common.Domain.EventSourcing`.

- [ ] **Step 3: Write the contract**

Create `src/Core/Common/Domain/EventSourcing/EventSourcedAggregate.Interface.cs`:

```csharp
using Core.Common.Domain.Events;

namespace Core.Common.Domain.EventSourcing;

/// <summary>An aggregate whose state is rebuilt from its event history.</summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public interface IEventSourcedAggregate<out TKey>
{
    /// <summary>Gets the aggregate identifier.</summary>
    TKey Id { get; }

    /// <summary>Gets the number of events applied to the aggregate.</summary>
    long Version { get; }

    /// <summary>Gets the events raised since the last successful append.</summary>
    IReadOnlyList<IDomainEvent> UncommittedEvents { get; }

    /// <summary>Replays history without marking events as uncommitted.</summary>
    void LoadFromHistory(long version, IEnumerable<IDomainEvent> history);

    /// <summary>Clears the events after a successful append.</summary>
    void ClearUncommittedEvents();
}
```

- [ ] **Step 4: Write the base**

Create `src/Core/Common/Domain/EventSourcing/EventSourcedAggregate.Base.cs`:

```csharp
using Core.Common.Domain.Events;

namespace Core.Common.Domain.EventSourcing;

/// <summary>
/// Base aggregate that applies events through a single <see cref="When"/> switch.
/// </summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public abstract class EventSourcedAggregate<TKey> : IEventSourcedAggregate<TKey>
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    /// <inheritdoc />
    public TKey Id { get; protected set; } = default!;

    /// <inheritdoc />
    public long Version { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    /// <inheritdoc />
    public void LoadFromHistory(long version, IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        foreach (IDomainEvent domainEvent in history)
            When(domainEvent);

        Version = version;
    }

    /// <summary>Applies a new event and records it for the next append.</summary>
    /// <param name="domainEvent">The event to apply.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        When(domainEvent);
        Version++;
        _uncommittedEvents.Add(domainEvent);
    }

    /// <summary>Applies one event to the aggregate state.</summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <exception cref="InvalidOperationException">The event type is not handled.</exception>
    protected abstract void When(IDomainEvent domainEvent);

    /// <inheritdoc />
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
```

Create `src/Core/Common/Domain/EventSourcing/AggregateFactory.Interface.cs`:

```csharp
namespace Core.Common.Domain.EventSourcing;

/// <summary>Creates an empty aggregate for rehydration.</summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public interface IAggregateFactory<out TAggregate>
{
    /// <summary>Creates an empty aggregate with no events applied.</summary>
    TAggregate Create();
}
```

- [ ] **Step 5: Run the test and verify it passes**

Run:

```bash
dotnet build tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build --filter-class "Core.Unit.Tests.Common.Domain.EventSourcing.EventSourcedAggregateTest"
```

Expected: PASS, 9 tests.

- [ ] **Step 6: Commit**

```bash
git add src/Core/Common/Domain/EventSourcing tests/Unit/Core.Unit.Tests/Common/Domain/EventSourcing
git commit -m "feat(core): add event-sourced aggregate base"
```

---

### Task 6: Full Slice Verification

**Files:**
- Modify: `docs/specs/2026-10-29-domain-design-closure-design.md` only if implementation diverged.
- No production files unless a gate fails.

**Interfaces:**
- Consumes: every contract produced by Tasks 1–5.
- Produces: a green slice baseline for slice ②.

- [ ] **Step 1: Format**

Run:

```bash
make format
```

Expected: no errors.

- [ ] **Step 2: Lint and structure**

Run:

```bash
make lint
```

Expected: `structure check passed`, `dotnet format` clean, `vue-tsc` clean.

- [ ] **Step 3: Build**

Run:

```bash
make build
```

Expected: solution and both SPA bundles succeed with zero warnings.

- [ ] **Step 4: Run the fast test gate**

Run:

```bash
make test
```

Expected: every .NET and web suite passes; 0 failed.

- [ ] **Step 5: Run unit coverage**

Run:

```bash
make coverage.unit
```

Expected:

```text
Core           >= 90.0% line / >= 80.0% branch   PASS
Application    >= 90.0% line / >= 80.0% branch   PASS
Infrastructure >= 90.0% line / >= 80.0% branch   PASS
```

- [ ] **Step 6: Run the API suite**

Run:

```bash
make api.test
```

Expected: PASS, 0 failed, 0 skipped.

- [ ] **Step 7: Run the PostgreSQL integration suites**

Run:

```bash
make infra.test
```

Expected: PASS, including `ConcurrencyTest`.

- [ ] **Step 8: Commit any gate fixes**

If formatting or a gate required a code change:

```bash
git add <only the files changed by the fix>
git commit -m "fix(core): close domain design gate findings"
```

If no files changed, skip this step.

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| Specifications | Task 1 |
| Domain policies | Task 2 |
| Event identity and versioning | Task 3 |
| Optimistic concurrency + migration | Task 4 |
| Event-sourced aggregate base + factory | Task 5 |
| Acceptance gates | Task 6 |

**Placeholder scan:** no `TBD`, `TODO`, or “similar to Task N”.

**Type consistency:** `ISpecification<T>`, `DomainPolicy<TContext,TResult>`,
`IDomainEvent.EventName/EventVersion`, `IAggregate.Version`,
`IEventSourcedAggregate<TKey>`, `IAggregateFactory<TAggregate>` are named identically in
the task that defines them and the task that consumes them.

## Execution Handoff

Plan complete and saved to `docs/plans/2026-10-29-domain-design-closure.md`. Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks.
2. **Inline Execution** — execute tasks in this session with checkpoints.
