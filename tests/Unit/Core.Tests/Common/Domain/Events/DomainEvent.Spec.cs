using Core.Common.Domain.Events;

namespace Core.Tests.Common.Domain.Events;

/// <summary>
/// Tests for the <see cref="DomainEvent"/> metadata envelope:
/// well-known trace identifiers populated by the constructor.
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventSpec
{
    private static readonly Guid FixedEventId =
        Guid.Parse("cccccccc-1111-2222-3333-444444444444");

    private static readonly Guid OtherEventId =
        Guid.Parse("99999999-1111-2222-3333-444444444444");

    private static readonly Guid FixedCorrelationId =
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    private static readonly Guid FixedTenantId =
        Guid.Parse("eeeeeeee-1111-2222-3333-444444444444");

    private static readonly DateTimeOffset FixedOccurredOnUtc =
        new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset OtherOccurredOnUtc =
        new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record TestDomainEvent(
        Guid? eventId = null,
        DateTimeOffset? occurredOnUtc = null,
        IMetadata? metadata = null)
        : DomainEvent(eventId, occurredOnUtc, metadata);

    [Fact]
    public void Constructor_AlwaysSetsEventIdAndTimestamp()
    {
        IDomainEvent evt = new TestDomainEvent();

        evt.GetEventId().Should().NotBeNull();
        evt.GetTimestamp().Should().NotBeNull();
    }

    [Fact]
    public void Constructor_UsesProvidedEnvelopeValues()
    {
        IDomainEvent evt = new TestDomainEvent(
            FixedEventId,
            FixedOccurredOnUtc);

        evt.GetEventId().Should().Be(FixedEventId);
        evt.GetTimestamp().Should().Be(FixedOccurredOnUtc);
    }

    [Fact]
    public void Constructor_MergesProvidedMetadataIntoEnvelope()
    {
        // Arrange
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.CorrelationId, FixedCorrelationId)
            .With(MetadataConstant.Keyword.TenantId, FixedTenantId);

        // Act
        IDomainEvent evt = new TestDomainEvent(metadata: metadata);

        // Assert
        evt.GetCorrelationId().Should().Be(FixedCorrelationId);
        evt.GetTenantId().Should().Be(FixedTenantId);
    }

    [Fact]
    public void Constructor_MetadataCannotOverrideExplicitEnvelopeParameters()
    {
        // Arrange
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.EventId, OtherEventId)
            .With(MetadataConstant.Keyword.Timestamp, OtherOccurredOnUtc);

        // Act
        IDomainEvent evt = new TestDomainEvent(
            FixedEventId,
            FixedOccurredOnUtc,
            metadata);

        // Assert
        evt.GetEventId().Should().Be(FixedEventId);
        evt.GetTimestamp().Should().Be(FixedOccurredOnUtc);
    }

    [Fact]
    public void Constructor_WithoutMetadata_OmitsCorrelationAndTenant()
    {
        IDomainEvent evt = new TestDomainEvent();

        evt.GetCorrelationId().Should().BeNull();
        evt.GetTenantId().Should().BeNull();
    }

    [Fact]
    public void Constructor_GeneratedEventId_ShouldBeUniquePerEvent()
    {
        IDomainEvent first = new TestDomainEvent();
        IDomainEvent second = new TestDomainEvent();

        var firstEventId = first.GetEventId();
        var secondEventId = second.GetEventId();

        firstEventId.Should().HaveValue();
        secondEventId.Should().HaveValue();
        firstEventId.Value.Should().NotBe(secondEventId.Value);
    }
}