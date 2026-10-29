using Application.Common.Messaging.Bus;

using Core.Common.Domain.Events;

using Infrastructure.Integrations.Messaging.Serialization;

namespace Infrastructure.Unit.Tests.Integrations.Messaging.Serialization;

/// <summary>
/// Unit tests for <see cref="EventTypeRegistry"/>; covers assembly scanning and explicit registration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging.Serialization")]
public sealed class EventTypeRegistryTest
{
    [Fact(DisplayName = "Constructor should register domain events found in the scanned assembly")]
    public void Constructor_WithAssemblyContainingDomainEvents_ShouldRegisterThem()
    {
        // Arrange
        var sut = new EventTypeRegistry(typeof(EventTypeRegistryTest).Assembly);

        // Act
        Type resolved = sut.Resolve(nameof(RegistryDomainEvent), 1);

        // Assert
        resolved.Should().Be<RegistryDomainEvent>();
    }

    [Fact(DisplayName = "Register should resolve an explicitly registered integration event")]
    public void Register_WithIntegrationEvent_ShouldResolveByStableIdentity()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 3);

        // Act
        Type resolved = sut.Resolve("registry.integration", 3);

        // Assert
        resolved.Should().Be<RegistryIntegrationEvent>();
    }

    [Fact(DisplayName = "Resolve should throw when the event name is unknown")]
    public void Resolve_WithUnknownEventName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Resolve("unknown.event", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*unknown.event*");
    }

    [Fact(DisplayName = "Resolve should throw when only the version differs")]
    public void Resolve_WithKnownNameAndUnknownVersion_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new EventTypeRegistry();
        sut.Register<RegistryIntegrationEvent>("registry.integration", 1);

        // Act
        Action act = () => sut.Resolve("registry.integration", 2);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*registry.integration*");
    }

    [Fact(DisplayName = "Resolve should throw when the event name is blank")]
    public void Resolve_WithBlankEventName_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Resolve(" ", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Register should reject a non-positive event version")]
    public void Register_WithNonPositiveVersion_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Register<RegistryIntegrationEvent>("registry.integration", 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Register should reject a blank event name")]
    public void Register_WithBlankEventName_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new EventTypeRegistry();

        // Act
        Action act = () => sut.Register<RegistryIntegrationEvent>("", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private sealed record RegistryDomainEvent : DomainEvent;

    private sealed record RegistryIntegrationEvent : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

        public string EventName => "registry.integration";

        public int Version => 1;

        public DefaultMetadata Metadata { get; init; } = DefaultMetadata.Create();
    }
}
