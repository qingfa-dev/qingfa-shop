using Application.BuildingBlocks.EventSourcing;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Functional.Maybes;
using Core.BuildingBlocks.Functional.Results;

using Infrastructure.Integration.Tests.BuildingBlocks.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.BuildingBlocks.EventSourcing;

/// <summary>PostgreSQL event-store, snapshot, and checkpoint integration tests.</summary>
[Collection("messaging")]
public sealed class EventStoreIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
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
    public async Task AppendAndRead_ShouldPreserveStreamOrderAndGlobalPositions()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        string streamId = $"test-{Guid.NewGuid():N}";
        var first = new TestStoreEvent("first");
        var second = new TestStoreEvent("second");

        // Act
        Result<long> append = await store.AppendAsync(streamId, 0, [first, second]);
        Result<IReadOnlyList<StoredEvent>> stream = await store.ReadStreamAsync(streamId, 1);
        Result<IReadOnlyList<StoredEvent>> all = await store.ReadAllAsync(0, 100);

        // Assert
        append.IsSuccess.Should().BeTrue();
        append.Value.Should().Be(2);
        stream.IsSuccess.Should().BeTrue();
        stream.Value.Should().HaveCount(2);
        stream.Value.Select(stored => stored.Version).Should().Equal(1, 2);
        all.IsSuccess.Should().BeTrue();
        all.Value.Should().Contain(stored => stored.StreamId == streamId);
    }

    [Fact]
    public async Task Append_WithStaleExpectedVersion_ShouldReturnConflict()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        string streamId = $"test-{Guid.NewGuid():N}";
        await store.AppendAsync(streamId, 0, [new TestStoreEvent("first")]);

        // Act
        Result<long> stale = await store.AppendAsync(streamId, 0, [new TestStoreEvent("stale")]);

        // Assert
        stale.IsFailure.Should().BeTrue();
        stale.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(EventSourcingOutcome.Codes.AppendConflict);
    }

    [Fact]
    public async Task SnapshotAndCheckpoint_ShouldRoundTrip()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        var snapshots = scope.ServiceProvider.GetRequiredService<ISnapshotStore>();
        var checkpoints = scope.ServiceProvider.GetRequiredService<IProjectionCheckpointStore>();
        string streamId = $"test-{Guid.NewGuid():N}";
        var snapshot = new Snapshot(streamId, 7, "{\"value\":1}", DateTimeOffset.UtcNow);
        string projection = $"projection-{Guid.NewGuid():N}";

        // Act
        Result savedSnapshot = await snapshots.SaveAsync(snapshot);
        Result<Maybe<Snapshot>> loadedSnapshot = await snapshots.LoadAsync(streamId);
        Result savedCheckpoint = await checkpoints.SavePositionAsync(projection, 42);
        Result<long> loadedCheckpoint = await checkpoints.GetPositionAsync(projection);

        // Assert
        savedSnapshot.IsSuccess.Should().BeTrue();
        loadedSnapshot.IsSuccess.Should().BeTrue();
        loadedSnapshot.Value.Value.Version.Should().Be(7);
        savedCheckpoint.IsSuccess.Should().BeTrue();
        loadedCheckpoint.Value.Should().Be(42);
    }

    private sealed record TestStoreEvent(string Value) : DomainEvent;
}
