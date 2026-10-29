namespace Application.Common.Messaging.Outbox;

/// <summary>Failures produced by the outbox pipeline.</summary>
public static class OutboxOutcome
{
    public static class Failure
    {
        public static Error EnqueueFailed(Exception exception) => Error.InternalServerError(
            Codes.EnqueueFailed, exception.Message);

        public static Error ClaimFailed(Exception exception) => Error.InternalServerError(
            Codes.ClaimFailed, exception.Message);

        public static Error PublishFailed(OutboxMessage message, string reason) => Error.InternalServerError(
            Codes.PublishFailed, $"Outbox message '{message.Id}' could not be published: {reason}");

        public static Error DeserializeFailed(OutboxMessage message, string reason) => Error.InternalServerError(
            Codes.DeserializeFailed, $"Outbox message '{message.Id}' could not be deserialized: {reason}");

        public static Error MarkFailed(Guid id, string reason) => Error.InternalServerError(
            Codes.MarkFailed, $"Outbox message '{id}' could not be updated: {reason}");

        public static Error DeadLetterFailed(Guid id, string reason) => Error.InternalServerError(
            Codes.DeadLetterFailed, $"Outbox message '{id}' could not be dead-lettered: {reason}");

        public static Error NotFound(Guid id) => Error.NotFound(
            Codes.NotFound, $"Outbox message '{id}' was not found.");
    }

    public static class Codes
    {
        public const string EnqueueFailed = "Outbox.Enqueue.Failed";
        public const string ClaimFailed = "Outbox.Claim.Failed";
        public const string SerializeFailed = "Outbox.Serialize.Failed";
        public const string DeserializeFailed = "Outbox.Deserialize.Failed";
        public const string PublishFailed = "Outbox.Publish.Failed";
        public const string MarkFailed = "Outbox.Mark.Failed";
        public const string DeadLetterFailed = "Outbox.DeadLetter.Failed";
        public const string NotFound = "Outbox.NotFound";
    }
}
