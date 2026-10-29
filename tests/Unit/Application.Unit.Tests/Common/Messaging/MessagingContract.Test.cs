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
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(0);
        message.Version.Should().Be(1);
    }

    [Fact]
    public void OutboxOutcome_CodesAreStable()
    {
        OutboxOutcome.Codes.EnqueueFailed.Should().Be("Outbox.Enqueue.Failed");
        OutboxOutcome.Codes.ClaimFailed.Should().Be("Outbox.Claim.Failed");
        OutboxOutcome.Codes.PublishFailed.Should().Be("Outbox.Publish.Failed");
        OutboxOutcome.Codes.DeadLetterFailed.Should().Be("Outbox.DeadLetter.Failed");
    }
}
