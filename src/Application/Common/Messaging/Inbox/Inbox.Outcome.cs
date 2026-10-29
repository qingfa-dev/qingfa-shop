namespace Application.Common.Messaging.Inbox;

/// <summary>Failures produced by the inbox pipeline.</summary>
public static class InboxOutcome
{
    public static class Failure
    {
        public static Error ExecutionFailed(Exception exception) => Error.FromException(
            exception, Codes.ExecutionFailed);
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ExecutionFailed = "Inbox.Execution.Failed";
    }
}