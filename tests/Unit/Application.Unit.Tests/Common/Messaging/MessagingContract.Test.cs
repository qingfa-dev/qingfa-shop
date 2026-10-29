using Application.Common.Messaging.Outbox;

namespace Application.Unit.Tests.Common.Messaging;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Messaging")]
public sealed class MessagingContractTest
{
    [Fact]
    public void OutboxMessage_DefaultsToPendingWithAGeneratedId()
    {
        var message = new OutboxMessage();

        message.Id.Should().NotBe(Guid.Empty);
        message.Kind.Should().Be(OutboxMessageKind.Domain);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(0);
        message.Version.Should().Be(1);
        message.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OutboxOutcome_CodesAreStable()
    {
        OutboxOutcome.Codes.EnqueueFailed.Should().Be("Outbox.Enqueue.Failed");
        OutboxOutcome.Codes.ClaimFailed.Should().Be("Outbox.Claim.Failed");
        OutboxOutcome.Codes.SerializeFailed.Should().Be("Outbox.Serialize.Failed");
        OutboxOutcome.Codes.DeserializeFailed.Should().Be("Outbox.Deserialize.Failed");
        OutboxOutcome.Codes.PublishFailed.Should().Be("Outbox.Publish.Failed");
        OutboxOutcome.Codes.MarkFailed.Should().Be("Outbox.Mark.Failed");
        OutboxOutcome.Codes.LeaseLost.Should().Be("Outbox.Lease.Lost");
        OutboxOutcome.Codes.DeadLetterFailed.Should().Be("Outbox.DeadLetter.Failed");
        OutboxOutcome.Codes.NotFound.Should().Be("Outbox.NotFound");
    }
}
