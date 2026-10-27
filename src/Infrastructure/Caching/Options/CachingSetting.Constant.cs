namespace Infrastructure.Caching.Options;

/// <summary>Constants for the root caching configuration section.</summary>
public static class CachingSettingConstant
{
    /// <summary>The configuration section that holds the caching settings.</summary>
    public const string SectionName = "Caching";

    /// <summary>Default values applied when the configuration omits a setting.</summary>
    public static class Defaults
    {
        /// <summary>Caching is enabled unless a host turns it off.</summary>
        public const bool Enabled = true;
    }

}