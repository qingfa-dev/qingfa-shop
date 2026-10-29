using System.Text.Json;

using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;
using Core.Todos.Items;

using Infrastructure.Integrations.Messaging.Serialization;

namespace Infrastructure.Unit.Tests.Integrations.Messaging.Serialization;

/// <summary>
/// Unit tests for <see cref="JsonMessageSerializer"/>; covers the domain and integration envelopes.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging.Serialization")]
public sealed class JsonMessageSerializerTest
{
    private const string IntegrationEventName = "test.integration";
    private const int IntegrationEventVersion = 1;

    private readonly IDomainEventSerializer _domainSerializer;
    private readonly IIntegrationEventSerializer _integrationSerializer;

    public JsonMessageSerializerTest()
    {
        var registry = new EventTypeRegistry(typeof(TodoCreated).Assembly);
        registry.Register<TestDomainEvent>(nameof(TestDomainEvent), 1);
        registry.Register<TestIntegrationEvent>(IntegrationEventName, IntegrationEventVersion);

        var serializer = new JsonMessageSerializer(registry);
        _domainSerializer = serializer;
        _integrationSerializer = serializer;
    }

    [Fact(DisplayName = "Domain event round-trip should preserve the envelope and metadata")]
    public void DomainEventRoundTrip_ShouldPreserveEnvelopeAndMetadata()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("order-placed", TestPriority.High)
        {
            Metadata = DefaultMetadata.Create().With("tenant", "acme")
        };

        // Act
        string payload = _domainSerializer.Serialize(domainEvent);
        IDomainEvent result = _domainSerializer.Deserialize(
            domainEvent.EventName,
            domainEvent.EventVersion,
            payload);

        // Assert
        result.Should().BeOfType<TestDomainEvent>();
        result.EventName.Should().Be(nameof(TestDomainEvent));
        result.EventVersion.Should().Be(1);
        result.Metadata["tenant"].Should().Be("acme");
        result.As<TestDomainEvent>().Name.Should().Be("order-placed");
        result.As<TestDomainEvent>().Priority.Should().Be(TestPriority.High);
    }

    [Fact(DisplayName = "Real TodoCreated round-trip should preserve the metadata envelope")]
    public void DomainEventRoundTrip_WithTodoCreated_ShouldPreserveMetadataEnvelope()
    {
        // Arrange
        Guid todoId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        var created = new TodoCreated(todoId, tenantId, "Buy milk");
        created.SetTenantId(tenantId);
        Guid eventId = created.GetEventId()!.Value;
        DateTimeOffset timestamp = created.GetTimestamp()!.Value;

        // Act
        string payload = _domainSerializer.Serialize(created);
        IDomainEvent result = _domainSerializer.Deserialize(created.EventName, created.EventVersion, payload);

        // Assert
        result.Should().BeOfType<TodoCreated>();
        result.GetEventId().Should().Be(eventId);
        result.GetTenantId().Should().Be(tenantId);
        result.GetTimestamp().Should().Be(timestamp);
        result.As<TodoCreated>().TodoId.Should().Be(todoId);
        result.As<TodoCreated>().Title.Should().Be("Buy milk");
    }

    [Fact(DisplayName = "Metadata scalars should round-trip as CLR values rather than JsonElement")]
    public void DomainEventRoundTrip_ShouldNormalizeMetadataScalars()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("order-placed", TestPriority.Low)
        {
            Metadata = DefaultMetadata.Create()
                .With("tenant", "acme")
                .With("attempts", 3)
                .With("enabled", true)
        };

        // Act
        string payload = _domainSerializer.Serialize(domainEvent);
        IDomainEvent result = _domainSerializer.Deserialize(
            domainEvent.EventName,
            domainEvent.EventVersion,
            payload);

        // Assert
        result.Metadata["tenant"].Should().Be("acme");
        result.Metadata["attempts"].Should().BeOfType<long>().Which.Should().Be(3L);
        result.Metadata["enabled"].Should().Be(true);
    }

    [Fact(DisplayName = "Integration event round-trip should preserve the envelope and metadata")]
    public void IntegrationEventRoundTrip_ShouldPreserveEnvelopeAndMetadata()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            IntegrationEventName,
            IntegrationEventVersion,
            "recipient@example.com",
            DefaultMetadata.Create().With("tenant", "acme"));

        // Act
        string payload = _integrationSerializer.Serialize(integrationEvent);
        IIntegrationEvent result = _integrationSerializer.Deserialize(
            IntegrationEventName,
            IntegrationEventVersion,
            payload);

        // Assert
        result.Should().BeOfType<TestIntegrationEvent>();
        result.MessageId.Should().Be(integrationEvent.MessageId);
        result.EventName.Should().Be(IntegrationEventName);
        result.Version.Should().Be(IntegrationEventVersion);
        result.Metadata["tenant"].Should().Be("acme");
        result.As<TestIntegrationEvent>().Recipient.Should().Be("recipient@example.com");
    }

    [Fact(DisplayName = "Deserialize should throw when the event name is not registered")]
    public void Deserialize_WithUnknownEventName_ShouldThrowInvalidOperationException()
    {
        // Act
        Action act = () => _domainSerializer.Deserialize("unknown.event", 1, "{}");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*unknown.event*");
    }

    [Fact(DisplayName = "Deserialize should throw when the resolved type is not a domain event")]
    public void Deserialize_WithNonDomainEventName_ShouldThrowInvalidOperationException()
    {
        // Act
        Action act = () => _domainSerializer.Deserialize(typeof(string).AssemblyQualifiedName!, 1, "\"text\"");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*IDomainEvent*");
    }

    [Fact(DisplayName = "Metadata should serialize as a JSON object map without a dictionary wrapper")]
    public void Serialize_WithMetadata_ShouldWriteFlatJsonObject()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("order-placed", TestPriority.Normal)
        {
            Metadata = DefaultMetadata.Create().With("tenant", "acme")
        };

        // Act
        string payload = _domainSerializer.Serialize(domainEvent);

        // Assert
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement metadata = document.RootElement.GetProperty("metadata");
        metadata.ValueKind.Should().Be(JsonValueKind.Object);
        metadata.GetProperty("tenant").GetString().Should().Be("acme");
        metadata.EnumerateObject().Should().ContainSingle();
    }

    [Fact(DisplayName = "Enums should serialize as strings")]
    public void Serialize_WithEnumProperty_ShouldWriteStringValue()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("order-placed", TestPriority.High);

        // Act
        string payload = _domainSerializer.Serialize(domainEvent);

        // Assert
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement priority = document.RootElement.GetProperty("priority");
        priority.ValueKind.Should().Be(JsonValueKind.String);
        priority.GetString().Should().Be("High");
    }

    private enum TestPriority
    {
        Low,
        Normal,
        High
    }

    private sealed record TestDomainEvent(string Name, TestPriority Priority) : DomainEvent;

    private sealed record TestIntegrationEvent(
        Guid MessageId,
        DateTimeOffset OccurredOnUtc,
        string EventName,
        int Version,
        string Recipient,
        DefaultMetadata Metadata) : IIntegrationEvent;
}
