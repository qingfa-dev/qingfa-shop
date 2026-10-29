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
