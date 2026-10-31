namespace Application.BuildingBlocks.Security.Identity.External.Options;

/// <summary>Constants for external authentication option validation.</summary>
public static class ExternalAuthConstant
{
    /// <summary>Validation bounds enforced by <see cref="ExternalOptionValidator"/>.</summary>
    public static class Constraints
    {
        /// <summary>A one-time sign-in code must live at least one second.</summary>
        public const int CodeLifetimeSecondsMin = 1;

        /// <summary>Upper bound for a one-time sign-in code, in seconds.</summary>
        public const int CodeLifetimeSecondsMax = 3600;
    }
}
