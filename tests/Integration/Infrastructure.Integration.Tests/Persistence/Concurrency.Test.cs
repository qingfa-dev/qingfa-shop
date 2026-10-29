using Application.Common.Persistence.UnitOfWorks;

using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Integration.Tests.Persistence.Todos;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Interceptors;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Integration.Tests.Persistence;

/// <summary>Proves that a stale aggregate update is rejected by PostgreSQL.</summary>
[Collection("Todo EfCore")]
public sealed class ConcurrencyTest(TodoEfCoreFixture fixture) : IAsyncLifetime
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await fixture.CleanAsync();
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

        using var context1 = CreateContext();
        using var context2 = CreateContext();

        context1.Add(todo);
        await context1.SaveChangesAsync();
        context1.ChangeTracker.Clear();

        Todo first = await context1.Set<Todo>().SingleAsync(entity => entity.Id == todo.Id);
        Todo second = await context2.Set<Todo>().SingleAsync(entity => entity.Id == todo.Id);

        // Act: the versionable interceptor moves the first writer's row to version 1.
        first.Title = "first";
        await context1.SaveChangesAsync();

        // Assert: the interceptor, not the test, produced the new version.
        first.Version.Should().Be(1);

        // Act: the second writer still believes it is version 0.
        second.Title = "second";
        var unitOfWork = new EfCoreUnitOfWork(context2);
        Result result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.ConcurrencyConflict);
    }

    /// <summary>
    /// Builds a context with the same provider options and the versionable interceptor the
    /// production registration applies, so the test exercises the real version bump.
    /// </summary>
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql
                .UsePersistenceMigrationHistory()
                .CommandTimeout(30))
            .AddInterceptors(new VersionableInterceptor())
            .Options;

        return new ApplicationDbContext(options);
    }
}
