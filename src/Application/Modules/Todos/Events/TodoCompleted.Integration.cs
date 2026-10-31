using System.Text.Json.Serialization;

using Application.BuildingBlocks.Messaging.Events;

namespace Application.Modules.Todos.Events;

/// <summary>Published when a todo is completed so other processes can react.</summary>
public sealed record TodoCompletedIntegration : IntegrationEvent
{
    /// <summary>Gets the identifier of the completed todo.</summary>
    public Guid TodoId { get; init; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Gets the UTC date the todo was completed.</summary>
    public DateTimeOffset CompletedAtUtc { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string EventName => TodoIntegrationConstant.Events.Completed;

    /// <inheritdoc />
    [JsonIgnore]
    public override int Version => 1;
}