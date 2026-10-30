using Application.Common.EventSourcing;

using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Integration.Tests.Persistence.Todos;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Persistence;

/// <summary>Proves that a stale event-stream append is rejected by PostgreSQL.</summary>
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
    public async Task StaleUpdate_ReturnsAppendConflict()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            tenantId,
            "concurrency",
            nowUtc).Value;
        Result created = await fixture.TodoEvents.SaveAsync(todo);
        created.IsSuccess.Should().BeTrue();

        using IServiceScope firstScope = fixture.Services.CreateScope();
        using IServiceScope secondScope = fixture.Services.CreateScope();

        var firstRepository = firstScope.ServiceProvider
            .GetRequiredService<IEventSourcedRepository<Todo, Guid>>();
        var secondRepository = secondScope.ServiceProvider
            .GetRequiredService<IEventSourcedRepository<Todo, Guid>>();

        Todo first = (await firstRepository.LoadAsync(todo.Id)).Value;
        Todo second = (await secondRepository.LoadAsync(todo.Id)).Value;
        DateTimeOffset updateNowUtc = nowUtc.AddMinutes(1);

        // Act: the first writer appends at the version both readers observed.
        first.Update(
            "first",
            first.Description,
            first.Priority,
            first.DueAtUtc,
            first.ListId,
            updateNowUtc,
            "first-writer").IsSuccess.Should().BeTrue();
        Result firstUpdate = await firstRepository.SaveAsync(first);

        // Assert: the stream accepted the first append.
        firstUpdate.IsSuccess.Should().BeTrue(
            string.Join(", ", firstUpdate.Errors.Select(error => $"{error.Code}: {error.Message}")));

        // Act: the second writer still believes it is the version both readers loaded.
        second.Update(
            "second",
            second.Description,
            second.Priority,
            second.DueAtUtc,
            second.ListId,
            updateNowUtc,
            "second-writer").IsSuccess.Should().BeTrue();
        Result staleUpdate = await secondRepository.SaveAsync(second);

        // Assert: the stale append fails instead of overwriting.
        staleUpdate.IsFailure.Should().BeTrue();
        staleUpdate.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.AppendConflict);

        // Assert: a fresh load still sees the first writer's event.
        using IServiceScope verificationScope = fixture.Services.CreateScope();
        var verificationRepository = verificationScope.ServiceProvider
            .GetRequiredService<IEventSourcedRepository<Todo, Guid>>();
        Todo persisted = (await verificationRepository.LoadAsync(todo.Id)).Value;

        persisted.Title.Should().Be("first");
        persisted.Version.Should().Be(2);
    }
}
