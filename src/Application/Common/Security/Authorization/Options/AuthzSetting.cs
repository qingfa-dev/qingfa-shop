namespace Application.Common.Security.Authorization.Options;

/// <summary>
/// Configuration options for the authorization system.
/// </summary>
public sealed class AuthzSetting
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Gets or sets the permission cache configuration options.
    /// </summary>
    public PermissionCacheOptions PermissionCache { get; init; } = new();
}

/// <summary>
/// Configuration options for the permission cache behavior.
/// </summary>
public class PermissionCacheOptions
{
    /// <summary>
    /// Gets or sets the sliding expiration time for cached permissions.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan SlidingExpiration { get; init; } = TimeSpan.FromMinutes(
        AuthzSettingConstant.Defaults.PermissionCacheSlidingExpirationMinutes);

    /// <summary>
    /// Gets or sets the absolute expiration time for the local cache.
    /// Defaults to 30 minutes.
    /// </summary>
    public TimeSpan AbsoluteExpiration { get; init; } = TimeSpan.FromMinutes(
        AuthzSettingConstant.Defaults.PermissionCacheAbsoluteExpirationMinutes);
}
