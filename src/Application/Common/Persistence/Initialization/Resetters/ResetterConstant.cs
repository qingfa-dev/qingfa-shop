namespace Application.Common.Persistence.Initialization.Resetters;

/// <summary>Constants for resetter operations.</summary>
public static class ResetterConstant
{
    /// <summary>Hard limits for resetters.</summary>
    public static class Limits
    {
        /// <summary>Default order for resetters when not explicitly set.</summary>
        public const int DefaultOrder = 100;

        /// <summary>Maximum number of resetters that can be registered.</summary>
        public const int MaxResetters = 100;
    }

    /// <summary>Resetter status names.</summary>
    public static class Status
    {
        /// <summary>Resetter is pending execution.</summary>
        public const string Pending = "Pending";

        /// <summary>Resetter is currently executing.</summary>
        public const string Running = "Running";

        /// <summary>Resetter completed successfully.</summary>
        public const string Completed = "Completed";

        /// <summary>Resetter failed.</summary>
        public const string Failed = "Failed";

        /// <summary>Resetter was skipped.</summary>
        public const string Skipped = "Skipped";
    }

    /// <summary>Reset scope options.</summary>
    public static class Scope
    {
        /// <summary>Reset only data (truncate tables).</summary>
        public const string Data = "Data";

        /// <summary>Reset schema (drop and recreate).</summary>
        public const string Schema = "Schema";

        /// <summary>Reset both schema and data.</summary>
        public const string All = "All";
    }
}