namespace Application.BuildingBlocks.Security.Authorization.Options;

public static class AuthzSettingOutcome
{
    public static class Failure
    {
        public static Error PermissionCacheSlidingExpirationPositive => Error.Validation(
            code: Codes.PermissionCacheSlidingExpirationPositive,
            message:
            $"Permission cache sliding expiration must be at least {AuthzSettingConstant.Constraints.PermissionCacheSlidingExpirationMin} minutes.");

        public static Error PermissionCacheAbsoluteExpirationPositive => Error.Validation(
            code: Codes.PermissionCacheAbsoluteExpirationPositive,
            message:
            $"Permission cache absolute expiration must be at least {AuthzSettingConstant.Constraints.PermissionCacheAbsoluteExpirationMin} minutes.");

        public static Error PermissionCacheLocalExceedsAbsolute => Error.Validation(
            code: Codes.PermissionCacheLocalExceedsAbsolute,
            message: "Permission cache sliding expiration cannot exceed the absolute expiration.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string PermissionCacheSlidingExpirationPositive = "Authorization.PermissionCache.SlidingExpiration.Positive";
        public const string PermissionCacheAbsoluteExpirationPositive = "Authorization.PermissionCache.AbsoluteExpiration.Positive";
        public const string PermissionCacheLocalExceedsAbsolute = "Authorization.PermissionCache.LocalExceedsAbsolute";
    }
}