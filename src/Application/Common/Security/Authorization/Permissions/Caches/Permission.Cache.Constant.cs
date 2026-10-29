namespace Application.Common.Security.Authorization.Permissions.Caches;

// Context: Naming patterns, default TTL values, and boundary constraints for permission cache operations.
public static class PermissionCacheConstant
{
    public static class Constraints
    {
        public const int MaxKeyLength = 256;
        public const int MaxTagLength = 128;
    }

    public static class Defaults
    {
        public const double SlidingExpirationMinutes = 5;
        public const double AbsoluteExpirationMinutes = 30;

        /// <summary>Lifetime of a negative (miss) marker so a failing store is not retried on every request.</summary>
        public const int MissExpirationSeconds = 30;
    }

    public static class Patterns
    {
        public const string UserKeyPrefix = "perm:user:";
        public const string RoleKeyPrefix = "perm:role:";
        public const string GlobalTag = "perm:global";
        public const string UserTagPrefix = "perm:user:";
        public const string RoleTagPrefix = "perm:role:";
    }
}
