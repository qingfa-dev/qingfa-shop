using Application.Common.Events;

using Core.Common.Concerns.Foundation.Versionable;
using Core.Common.Concerns.Lifecycle.Activatable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.Publishable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

using Infrastructure.Persistence.EntityFramework.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Unit tests for the persistence interceptors. Each lifecycle test registers only the
/// interceptor under test so state transitions stay isolated.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class InterceptorTest
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(10);

    // ─── Lifecycle: Creatable ─────────────────────────────────────────────

    [Fact(DisplayName = "Lifecycle should stamp CreatedAtUtc on Added entities")]
    public async Task Lifecycle_WhenAdded_ShouldStampCreatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Lifecycle should leave CreatedAtUtc untouched on Modified entities")]
    public async Task Lifecycle_WhenModified_ShouldNotStampCreatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Name = "first" };
        context.Add(entity);
        await context.SaveChangesAsync();
        DateTimeOffset createdAt = entity.CreatedAtUtc;

        // Act
        entity.Name = "second";
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().Be(createdAt);
    }

    // ─── Lifecycle: Modifiable ────────────────────────────────────────────

    [Fact(DisplayName = "Lifecycle should stamp ModifiedAtUtc on Added entities")]
    public async Task Lifecycle_WhenAdded_ShouldStampModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Lifecycle should stamp ModifiedAtUtc on Modified entities")]
    public async Task Lifecycle_WhenModified_ShouldStampModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Name = "first" };
        context.Add(entity);
        await context.SaveChangesAsync();
        DateTimeOffset? previous = entity.ModifiedAtUtc;

        // Act
        var before = DateTimeOffset.UtcNow;
        entity.Name = "second";
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.Should().NotBe(previous);
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    // ─── Lifecycle: SoftDeletable ─────────────────────────────────────────

    [Fact(DisplayName = "Lifecycle should flag and timestamp deleted entities instead of removing them")]
    public async Task Lifecycle_WhenDeleted_ShouldSoftDeleteTheRow()
    {
        // Arrange
        string database = NewDatabaseName();
        await using (var seed = CreateContext(database))
        {
            seed.Add(new ConcernEntity { Id = Guid.NewGuid() });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(database, new LifecycleInterceptor());
        ConcernEntity entity = await context.Entities.SingleAsync();
        context.Remove(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
        entity.DeletedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
        context.ChangeTracker.Clear();
        ConcernEntity reloaded = await context.Entities.AsNoTracking().SingleAsync();
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact(DisplayName = "Lifecycle should turn the Deleted entry into an update")]
    public async Task Lifecycle_WhenDeleted_ShouldPersistTheSoftDelete()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        await context.SaveChangesAsync();
        context.Remove(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        context.Entry(entity).State.Should().Be(EntityState.Unchanged);
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Lifecycle should ignore Added and Modified entities when soft deleting")]
    public async Task Lifecycle_WhenAddedOrModified_ShouldLeaveTheDeleteFlagsAlone()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Name = "first" };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "second";
        await context.SaveChangesAsync();

        // Assert
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAtUtc.Should().BeNull();
    }

    // ─── Lifecycle: Activatable ───────────────────────────────────────────

    [Fact(DisplayName = "Lifecycle should stamp ActivatedAtUtc when IsActive becomes true")]
    public async Task Lifecycle_WhenActivated_ShouldStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = false };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var before = DateTimeOffset.UtcNow;
        entity.IsActive = true;
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().NotBeNull();
        entity.ActivatedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Lifecycle should not stamp when the entity was already active")]
    public async Task Lifecycle_WhenAlreadyActive_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "renamed";
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle should not stamp when an active entity is deactivated")]
    public async Task Lifecycle_WhenDeactivated_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.IsActive = false;
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle should not stamp on Added or Unchanged entities")]
    public async Task Lifecycle_WhenAddedOrUnchanged_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync();
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    // ─── Lifecycle: Publishable ───────────────────────────────────────────

    [Fact(DisplayName = "Lifecycle should stamp PublishedAtUtc when IsPublished becomes true")]
    public async Task Lifecycle_WhenPublished_ShouldStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = false };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var before = DateTimeOffset.UtcNow;
        entity.IsPublished = true;
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().NotBeNull();
        entity.PublishedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Lifecycle should not stamp when the entity was already published")]
    public async Task Lifecycle_WhenAlreadyPublished_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "renamed";
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle should not stamp when a published entity is unpublished")]
    public async Task Lifecycle_WhenUnpublished_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.IsPublished = false;
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle should not stamp on Added entities")]
    public async Task Lifecycle_WhenAdded_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new LifecycleInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    // ─── Foundation: Versionable ──────────────────────────────────────────

    [Fact(DisplayName = "Versionable should increment Version on Modified entities")]
    public async Task Versionable_WhenModified_ShouldIncrementVersion()
    {
        // Arrange
        await using var context = CreateContext(new VersionableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Name = "first" };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "second";
        await context.SaveChangesAsync();

        // Assert
        entity.Version.Should().Be(1);
    }

    [Fact(DisplayName = "Versionable should leave Version untouched on Added entities")]
    public async Task Versionable_WhenAdded_ShouldNotIncrementVersion()
    {
        // Arrange
        await using var context = CreateContext(new VersionableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Version = 7 };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.Version.Should().Be(7);
    }

    // ─── Domain events ────────────────────────────────────────────────────

    [Fact(DisplayName = "Domain events should publish every collected event after a successful save")]
    public async Task DomainEvents_WhenSaveSucceeds_ShouldPublishEveryEvent()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher();
        await using var context = CreateContext(new DomainEventInterceptor(publisher));
        var aggregate = new EventAggregate(Guid.NewGuid());
        var first = new TestDomainEvent("first");
        var second = new TestDomainEvent("second");
        aggregate.Raise(first);
        aggregate.Raise(second);
        context.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        publisher.Calls.Should().Be(1);
        publisher.Published.Should().Equal(first, second);
        aggregate.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Domain events should clear the aggregate after publishing")]
    public async Task DomainEvents_WhenPublishSucceeds_ShouldClearTheAggregate()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher();
        await using var context = CreateContext(new DomainEventInterceptor(publisher));
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("only"));
        context.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        aggregate.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Domain events should not publish when there are no events")]
    public async Task DomainEvents_WhenNoEventsExist_ShouldNotPublish()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher();
        await using var context = CreateContext(new DomainEventInterceptor(publisher));
        context.Add(new EventAggregate(Guid.NewGuid()));

        // Act
        await context.SaveChangesAsync();

        // Assert
        publisher.Calls.Should().Be(0);
        publisher.Published.Should().BeEmpty();
    }

    [Fact(DisplayName = "Domain events should not publish when the save fails")]
    public async Task DomainEvents_WhenSaveFails_ShouldNotPublish()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher();
        await using var context = CreateContext(
            new DomainEventInterceptor(publisher),
            new ThrowingSaveInterceptor());
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("rejected"));
        context.Add(aggregate);

        // Act
        Func<Task> act = () => context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        publisher.Calls.Should().Be(0);
        aggregate.Events.Should().ContainSingle();
    }

    [Fact(DisplayName = "Domain events should surface a publication failure and clear dispatched events")]
    public async Task DomainEvents_WhenPublicationFails_ShouldThrowResultException()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher
        {
            NextResult = Result.Fail(
                DomainEventOutcome.Failure.PublishFailed(typeof(TestDomainEvent), "handler failed"))
        };
        await using var context = CreateContext(new DomainEventInterceptor(publisher));
        var aggregate = new EventAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("failing"));
        context.Add(aggregate);

        // Act
        Func<Task> act = () => context.SaveChangesAsync();

        // Assert
        ResultException exception = (await act.Should().ThrowAsync<ResultException>()).Which;
        exception.Errors.Should().ContainSingle(error =>
            error.Code == DomainEventOutcome.Codes.PublishFailed);
        aggregate.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Domain events should publish on the synchronous save path")]
    public async Task DomainEvents_WhenSaveChangesIsSynchronous_ShouldPublish()
    {
        // Arrange
        var publisher = new RecordingDomainEventPublisher();
        await using var context = CreateContext(new DomainEventInterceptor(publisher));
        var aggregate = new EventAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("sync");
        aggregate.Raise(domainEvent);
        context.Add(aggregate);

        // Act
        context.SaveChanges();

        // Assert
        publisher.Calls.Should().Be(1);
        publisher.Published.Should().Equal(domainEvent);
        aggregate.Events.Should().BeEmpty();
    }

    // ─── Synchronous path ─────────────────────────────────────────────────

    [Fact(DisplayName = "SaveChanges (sync) should apply every registered interceptor")]
    public async Task SaveChanges_WhenCalledSynchronously_ShouldApplyEveryInterceptor()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(NewDatabaseName());
        builder.Apply(new RecordingDomainEventPublisher());
        await using var context = new InterceptorTestContext(builder.Options);
        var entity = new ConcernEntity
        {
            Id = Guid.NewGuid(),
            Name = "first",
            IsActive = false,
            IsPublished = false
        };
        context.Add(entity);

        // Act - Added
        context.SaveChanges();

        // Assert - Added
        entity.CreatedAtUtc.Should().NotBe(default);
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.Version.Should().Be(0);

        // Act - Modified
        entity.Name = "second";
        context.SaveChanges();

        // Assert - Modified
        entity.Version.Should().Be(1);

        // Act - Activated and published
        entity.IsActive = true;
        entity.IsPublished = true;
        context.SaveChanges();

        // Assert - Activated and published
        entity.ActivatedAtUtc.Should().NotBeNull();
        entity.PublishedAtUtc.Should().NotBeNull();
        entity.Version.Should().Be(2);

        // Act - Soft deleted
        context.Remove(entity);
        context.SaveChanges();

        // Assert - Soft deleted; the version also moves because Lifecycle turns the entry into an update.
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
        entity.Version.Should().Be(3);
        context.ChangeTracker.Clear();
        ConcernEntity reloaded = await context.Entities.AsNoTracking().SingleAsync();
        reloaded.IsDeleted.Should().BeTrue();
    }

    // ─── Registry ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Apply should register every persistence interceptor")]
    public void Apply_ShouldRegisterEveryPersistenceInterceptor()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(NewDatabaseName());

        // Act
        DbContextOptionsBuilder returned = builder.Apply(new RecordingDomainEventPublisher());

        // Assert
        returned.Should().BeSameAs(builder);
        IEnumerable<IInterceptor> interceptors = builder.Options
            .FindExtension<CoreOptionsExtension>()!
            .Interceptors!;
        interceptors
            .Select(interceptor => interceptor.GetType())
            .Should()
            .BeEquivalentTo(new[]
            {
                typeof(LifecycleInterceptor),
                typeof(VersionableInterceptor),
                typeof(DomainEventInterceptor),
            });
    }

    [Fact(DisplayName = "Apply should reject a null options builder")]
    public void Apply_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContextOptionsBuilder? builder = null;

        // Act
        Action act = () => builder!.Apply(new RecordingDomainEventPublisher());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Apply should reject a null publisher")]
    public void Apply_WhenPublisherIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(NewDatabaseName());

        // Act
        Action act = () => builder.Apply(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static string NewDatabaseName() => $"interceptors_{Guid.NewGuid():N}";

    private static InterceptorTestContext CreateContext(params IInterceptor[] interceptors)
        => CreateContext(NewDatabaseName(), interceptors);

    private static InterceptorTestContext CreateContext(string database, params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(database);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new InterceptorTestContext(builder.Options);
    }

    /// <summary>Records every publication attempt for assertions.</summary>
    private sealed class RecordingDomainEventPublisher : IDomainEventPublisher
    {
        public List<IDomainEvent> Published { get; } = [];

        public int Calls { get; private set; }

        public Result NextResult { get; set; } = Result.Ok();

        public Task<Result> PublishAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Published.AddRange(domainEvents);

            return Task.FromResult(NextResult);
        }
    }

    /// <summary>Fails the save before EF Core reaches the store.</summary>
    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("The save was rejected."));
    }
}

/// <summary>Entity implementing every persistence-relevant lifecycle and foundation concern.</summary>
public sealed class ConcernEntity :
    ICreatable,
    IModifiable,
    ISoftDeletable,
    IActivatable,
    IPublishable,
    IVersionable
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public string? ActivatedBy { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string? PublishedBy { get; set; }
    public long Version { get; set; }
}

/// <summary>Aggregate root used to exercise domain-event collection.</summary>
public sealed class EventAggregate : AggregateRoot<Guid>
{
    public EventAggregate()
        : base(Guid.NewGuid())
    {
    }

    public EventAggregate(Guid id)
        : base(id)
    {
    }

    public string? Name { get; set; }

    public void Raise(IDomainEvent domainEvent) => AddEvent(domainEvent);
}

/// <summary>Simple domain event used by the interceptor tests.</summary>
public sealed record TestDomainEvent(string Name) : DomainEvent;

/// <summary>Minimal DbContext used to exercise the interceptors.</summary>
public sealed class InterceptorTestContext(DbContextOptions<InterceptorTestContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<ConcernEntity> Entities => Set<ConcernEntity>();

    public DbSet<EventAggregate> Aggregates => Set<EventAggregate>();
}
