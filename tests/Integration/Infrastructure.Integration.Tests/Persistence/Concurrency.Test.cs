using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;

using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Integration.Tests.Persistence.Todos;

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
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            tenantId,
            "concurrency",
            nowUtc).Value;

        using IServiceScope createScope = fixture.Services.CreateScope();
        var createRepository = createScope.ServiceProvider
            .GetRequiredService<IPersistentRepository<Todo, Guid>>();
        var createUnitOfWork = createScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Result<Todo> created = await createRepository.SaveNewAsync(
            createUnitOfWork,
            todo,
            CancellationToken.None);
        created.IsSuccess.Should().BeTrue();

        using IServiceScope firstScope = fixture.Services.CreateScope();
        using IServiceScope secondScope = fixture.Services.CreateScope();

        var firstRepository = firstScope.ServiceProvider
            .GetRequiredService<IPersistentRepository<Todo, Guid>>();
        var secondRepository = secondScope.ServiceProvider
            .GetRequiredService<IPersistentRepository<Todo, Guid>>();
        var firstUnitOfWork = firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Todo first = (await firstRepository.GetByIdAsync(todo.Id)).Value;
        Todo second = (await secondRepository.GetByIdAsync(todo.Id)).Value;
        DateTimeOffset updateNowUtc = nowUtc.AddMinutes(1);

        // Act: the first writer commits and the versionable interceptor moves its row to version 1.
        first.Title = "first";
        Result<Todo> firstUpdate = await firstRepository.SaveUpdateAsync(
            firstUnitOfWork,
            first,
            updateNowUtc,
            "first-writer",
            CancellationToken.None);

        // Assert: the interceptor, not the test, produced the new version.
        firstUpdate.IsSuccess.Should().BeTrue(
            string.Join(", ", firstUpdate.Errors.Select(error => $"{error.Code}: {error.Message}")));
        first.Version.Should().Be(1);

        // Act: the second writer still believes it is version 0.
        second.Title = "second";
        Result<Todo> staleUpdate = await secondRepository.SaveUpdateAsync(
            secondUnitOfWork,
            second,
            updateNowUtc,
            "second-writer",
            CancellationToken.None);

        // Assert: the stale write fails instead of overwriting.
        staleUpdate.IsFailure.Should().BeTrue();
        staleUpdate.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.ConcurrencyConflict);

        // Assert: a fresh read still sees the first writer's row.
        using IServiceScope verificationScope = fixture.Services.CreateScope();
        var verificationRepository = verificationScope.ServiceProvider
            .GetRequiredService<IPersistentRepository<Todo, Guid>>();
        Todo persisted = (await verificationRepository.GetByIdAsync(todo.Id)).Value;

        persisted.Title.Should().Be("first");
        persisted.Version.Should().Be(1);
    }
}