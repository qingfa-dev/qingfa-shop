using Core.Common.Concerns.Foundation.Versionable;
using Core.Common.Concerns.Lifecycle.Activatable;
using Core.Common.Concerns.Lifecycle.Auditable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.Publishable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;

using Infrastructure.Persistence.EntityFramework.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Unit tests for the concern interceptors. Each test registers only the interceptor
/// under test so state transitions are isolated.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class InterceptorTest
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(10);

    // ─── Creatable ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Creatable should stamp CreatedAtUtc on Added entities")]
    public async Task Creatable_WhenAdded_ShouldStampCreatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new CreatableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Creatable should leave CreatedAtUtc untouched on Modified entities")]
    public async Task Creatable_WhenModified_ShouldNotStampCreatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new CreatableInterceptor());
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

    // ─── Modifiable ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Modifiable should stamp ModifiedAtUtc on Added entities")]
    public async Task Modifiable_WhenAdded_ShouldStampModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ModifiableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Modifiable should stamp ModifiedAtUtc on Modified entities")]
    public async Task Modifiable_WhenModified_ShouldStampModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ModifiableInterceptor());
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

    [Fact(DisplayName = "Modifiable should ignore Deleted entities")]
    public async Task Modifiable_WhenDeleted_ShouldNotStampModifiedAtUtc()
    {
        // Arrange
        string database = NewDatabaseName();
        var modifiedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Guid id = Guid.NewGuid();
        await using (var seed = CreateContext(database))
        {
            seed.Add(new ConcernEntity { Id = id, ModifiedAtUtc = modifiedAt });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(database, new ModifiableInterceptor());
        ConcernEntity entity = await context.Entities.SingleAsync();
        context.Remove(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.Should().Be(modifiedAt);
    }

    // ─── Auditable ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Auditable should stamp creation and modification timestamps on Added entities")]
    public async Task Auditable_WhenAdded_ShouldStampCreatedAndModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new AuditableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid() };
        context.Add(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().BeCloseTo(before, Tolerance);
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    [Fact(DisplayName = "Auditable should only stamp ModifiedAtUtc on Modified entities")]
    public async Task Auditable_WhenModified_ShouldKeepCreatedAtUtcAndStampModifiedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new AuditableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), Name = "first" };
        context.Add(entity);
        await context.SaveChangesAsync();
        DateTimeOffset createdAt = entity.CreatedAtUtc;
        DateTimeOffset? previousModifiedAt = entity.ModifiedAtUtc;

        // Act
        var before = DateTimeOffset.UtcNow;
        entity.Name = "second";
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().Be(createdAt);
        entity.ModifiedAtUtc.Should().NotBe(previousModifiedAt);
        entity.ModifiedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
    }

    // ─── SoftDeletable ────────────────────────────────────────────────────

    [Fact(DisplayName = "SoftDeletable should flag and timestamp deleted entities instead of removing them")]
    public async Task SoftDeletable_WhenDeleted_ShouldSoftDeleteTheRow()
    {
        // Arrange
        string database = NewDatabaseName();
        await using (var seed = CreateContext(database))
        {
            seed.Add(new ConcernEntity { Id = Guid.NewGuid() });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(database, new SoftDeletableInterceptor());
        ConcernEntity entity = await context.Entities.SingleAsync();
        context.Remove(entity);
        var before = DateTimeOffset.UtcNow;

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
        entity.DeletedAtUtc!.Value.Should().BeCloseTo(before, Tolerance);
        context.ChangeTracker.Clear();
        ConcernEntity reloaded = await context.Entities.AsNoTracking().SingleAsync();
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact(DisplayName = "SoftDeletable should turn the Deleted entry into an update")]
    public async Task SoftDeletable_WhenDeleted_ShouldPersistTheSoftDelete()
    {
        // Arrange
        await using var context = CreateContext(new SoftDeletableInterceptor());
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

    [Fact(DisplayName = "SoftDeletable should ignore Added and Modified entities")]
    public async Task SoftDeletable_WhenAddedOrModified_ShouldLeaveTheFlagsAlone()
    {
        // Arrange
        await using var context = CreateContext(new SoftDeletableInterceptor());
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

    // ─── Activatable ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Activatable should stamp ActivatedAtUtc when IsActive becomes true")]
    public async Task Activatable_WhenActivated_ShouldStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ActivatableInterceptor());
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

    [Fact(DisplayName = "Activatable should not stamp when the entity was already active")]
    public async Task Activatable_WhenAlreadyActive_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ActivatableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "renamed";
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Activatable should not stamp when an active entity is deactivated")]
    public async Task Activatable_WhenDeactivated_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ActivatableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.IsActive = false;
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Activatable should not stamp on Added or Unchanged entities")]
    public async Task Activatable_WhenAddedOrUnchanged_ShouldNotStampActivatedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new ActivatableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsActive = true };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync();
        await context.SaveChangesAsync();

        // Assert
        entity.ActivatedAtUtc.Should().BeNull();
    }

    // ─── Publishable ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Publishable should stamp PublishedAtUtc when IsPublished becomes true")]
    public async Task Publishable_WhenPublished_ShouldStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new PublishableInterceptor());
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

    [Fact(DisplayName = "Publishable should not stamp when the entity was already published")]
    public async Task Publishable_WhenAlreadyPublished_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new PublishableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "renamed";
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Publishable should not stamp when a published entity is unpublished")]
    public async Task Publishable_WhenUnpublished_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new PublishableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.IsPublished = false;
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Publishable should not stamp on Added entities")]
    public async Task Publishable_WhenAdded_ShouldNotStampPublishedAtUtc()
    {
        // Arrange
        await using var context = CreateContext(new PublishableInterceptor());
        var entity = new ConcernEntity { Id = Guid.NewGuid(), IsPublished = true };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.PublishedAtUtc.Should().BeNull();
    }

    // ─── Versionable ──────────────────────────────────────────────────────

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

    // ─── Synchronous path ─────────────────────────────────────────────────

    [Fact(DisplayName = "SaveChanges (sync) should apply every registered interceptor")]
    public async Task SaveChanges_WhenCalledSynchronously_ShouldApplyEveryInterceptor()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(NewDatabaseName());
        builder.Apply();
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

        // Act - Soft deleted
        context.Remove(entity);
        context.SaveChanges();

        // Assert - Soft deleted
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
        context.ChangeTracker.Clear();
        ConcernEntity reloaded = await context.Entities.AsNoTracking().SingleAsync();
        reloaded.IsDeleted.Should().BeTrue();
    }

    // ─── Registry ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Apply should register every concern interceptor")]
    public void Apply_ShouldRegisterEveryConcernInterceptor()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<InterceptorTestContext>()
            .UseInMemoryDatabase(NewDatabaseName());

        // Act
        DbContextOptionsBuilder returned = builder.Apply();

        // Assert
        returned.Should().BeSameAs(builder);
        builder.Options.FindExtension<CoreOptionsExtension>()!
            .Interceptors.Should().HaveCount(7);
    }

    [Fact(DisplayName = "Apply should reject a null options builder")]
    public void Apply_WhenNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContextOptionsBuilder? builder = null;

        // Act
        Action act = () => builder!.Apply();

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
}

/// <summary>Entity implementing every lifecycle, foundation, and version concern.</summary>
public sealed class ConcernEntity :
    ICreatable,
    IModifiable,
    IAuditable,
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

/// <summary>Minimal DbContext used to exercise the interceptors.</summary>
public sealed class InterceptorTestContext(DbContextOptions<InterceptorTestContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<ConcernEntity> Entities => Set<ConcernEntity>();
}
