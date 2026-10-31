namespace Application.BuildingBlocks.Ports.Storage.Security.Guard;

public static class StorageAntiForgeryGuardOutcome
{
    public static class Failure
    {
        public static Error TooManyAttempts()
            => Error.Conflict(Codes.TooManyAttempts, "Upload blocked due to too many recent failures.");

        public static Error CacheUnavailable()
            => Error.ServiceUnavailable(Codes.CacheUnavailable, "The anti-forgery failure counter is unavailable.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TooManyAttempts = "Storage.TooManyAttempts";
        public const string CacheUnavailable = "Storage.AntiForgery.CacheUnavailable";
    }
}
