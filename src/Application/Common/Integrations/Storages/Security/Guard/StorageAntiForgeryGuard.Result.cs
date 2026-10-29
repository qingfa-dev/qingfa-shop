namespace Application.Common.Integrations.Storages.Security.Guard;

public static class StorageAntiForgeryGuardResult
{
    public static class Failure
    {
        public static Error TooManyAttempts()
            => Error.Conflict("Storage.TooManyAttempts", "Upload blocked due to too many recent failures.");

        public static Error CacheUnavailable()
            => Error.ServiceUnavailable("Storage.AntiForgery.CacheUnavailable", "The anti-forgery failure counter is unavailable.");
    }
}
