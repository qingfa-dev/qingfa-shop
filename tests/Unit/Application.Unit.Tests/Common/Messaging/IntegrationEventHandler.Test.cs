using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Events;
using Application.Common.Messaging.Handlers;
using Application.Common.Messaging.Inbox;

namespace Application.Unit.Tests.Common.Messaging;

/// <summary>Unit tests for the inbox-deduped consumer base.</summary>
[Trait("Category", "Unit")]
public sealed class IntegrationEventHandlerTest
{
    private static readonly Guid MessageId = Guid.Parse("4c1e7a92-8d3b-4f6a-b5c0-2e9d8f1a3b7c");

    #region Inbox

    [Fact(DisplayName = "Handle should run the consumer once through the inbox")]
    public async Task Handle_ShouldRunConsumerOnceThroughInbox()
    {
        var inbox = new RecordingInboxStore();
        var handler = new TestIntegrationEventHandler(inbox);
        var message = new TestIntegrationEvent { MessageId = MessageId };

        await handler.Handle(message, TestContext.Current.CancellationToken);

        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].MessageId.Should().Be(MessageId);
        inbox.Calls[0].Consumer.Should().Be(typeof(TestIntegrationEventHandler).FullName);
        inbox.Calls[0].Result.IsSuccess.Should().BeTrue();
        handler.Handled.Should().Be(1);
    }

    #endregion

    #region Failure

    [Fact(DisplayName = "Handle should throw ResultException when the consumer fails")]
    public async Task Handle_WhenConsumerFails_ShouldThrowResultException()
    {
        var inbox = new RecordingInboxStore();
        var handler = new TestIntegrationEventHandler(inbox)
        {
            Failure = Error.UnprocessableEntity("Test.Consumer.Rejected", "consumer rejected"),
        };
        var message = new TestIntegrationEvent { MessageId = MessageId };

        Func<Task> act = () => handler.Handle(message, TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<ResultException>();

        // Handle: the thrown failure carries the exception wrapped by the inbox rollback, so the
        // transport can retry the delivery.
        thrown.Which.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(InboxOutcome.Codes.ExecutionFailed);
        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].Result.IsFailure.Should().BeTrue();
        handler.Handled.Should().Be(1);
    }

    [Fact(DisplayName = "Handle should throw a returned inbox failure after it returns")]
    public async Task Handle_WhenInboxReturnsFailure_ShouldThrowResultException()
    {
        var failure = Result.Fail(
            InboxOutcome.Failure.ExecutionFailed(new InvalidOperationException("inbox begin failed")));
        var inbox = new RecordingInboxStore { ForcedResult = failure };
        var handler = new TestIntegrationEventHandler(inbox);

        Func<Task> act = () => handler.Handle(
            new TestIntegrationEvent { MessageId = MessageId },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
        handler.Handled.Should().Be(0);
        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].Result.Should().BeSameAs(failure);
    }

    #endregion

    #region Fakes

    private sealed record TestIntegrationEvent : IntegrationEvent
    {
        public override string EventName => "test.integration.event";
    }

    private sealed class TestIntegrationEventHandler(IInboxStore inbox)
        : IntegrationEventHandler<TestIntegrationEvent>(inbox)
    {
        public int Handled { get; private set; }

        public Error? Failure { get; init; }

        protected override Task<Result> HandleAsync(
            TestIntegrationEvent message,
            CancellationToken cancellationToken)
        {
            Handled++;

            return Task.FromResult(Failure is null ? Result.Ok() : Result.Fail(Failure));
        }
    }

    private sealed record Call(Guid MessageId, string Consumer, Result Result);

    private sealed class RecordingInboxStore : IInboxStore
    {
        public List<Call> Calls { get; } = [];

        public Result? ForcedResult { get; init; }

        public async Task<Result> ExecuteOnceAsync(
            Guid messageId,
            string consumer,
            Func<CancellationToken, Task<Result>> handler,
            CancellationToken cancellationToken = default)
        {
            if (ForcedResult is not null)
            {
                Calls.Add(new Call(messageId, consumer, ForcedResult));

                return ForcedResult;
            }

            try
            {
                Result result = await handler(cancellationToken);
                Calls.Add(new Call(messageId, consumer, result));

                return result;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Compensate: mimic the store, which rolls back and reports a thrown consumer.
                Result failure = Result.Fail(InboxOutcome.Failure.ExecutionFailed(exception));
                Calls.Add(new Call(messageId, consumer, failure));

                return failure;
            }
        }
    }

    #endregion
}