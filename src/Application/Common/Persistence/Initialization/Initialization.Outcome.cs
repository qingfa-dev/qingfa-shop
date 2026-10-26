namespace Application.Common.Persistence.Initialization;

public static class InitializationOutcome
{
    public static class Failure
    {
        public static Error MigratorsMissing => Error.InternalServerError(
            Codes.MigratorsMissing, "No database migrators are registered.");

        public static Error SchemaResetterMissing => Error.InternalServerError(
            Codes.SchemaResetterMissing, "Schema reset was requested, but no schema resetter is registered.");

        public static Error SchemaResetRejected(string reason) =>
            Error.Conflict(Codes.SchemaResetRejected, reason);

        public static Error MigrationFailed(string migratorName) =>
            Error.InternalServerError(Codes.MigrationFailed,
                $"Database initialization failed while applying migrations for '{migratorName}'.");

        public static Error SeedingFailed(int successful, int failed) =>
            Error.InternalServerError(Codes.SeedingFailed,
                $"Database initialization failed during seeding. Successful: {successful}; failed: {failed}.");

        public static Error Unexpected(string reason) =>
            Error.InternalServerError(Codes.Unexpected,
                $"An unexpected database initialization failure occurred: {reason}");

        public static Error NoStepsRegistered => Error.InternalServerError(
            Codes.NoStepsRegistered, "No initialization steps are registered.");

        public static Error DuplicateStepName(string name) => Error.InternalServerError(
            Codes.DuplicateStepName, $"Duplicate step name: '{name}'.");

        public static Error MissingDependency(string stepName, string dependencyName) => Error.InternalServerError(
            Codes.MissingDependency, $"Step '{stepName}' depends on missing step '{dependencyName}'.");

        public static Error StepFailed(string stepName, IReadOnlyList<Error> errors) =>
            Error.InternalServerError(Codes.StepFailed,
                $"Initialization step '{stepName}' failed: {string.Join("; ", errors.Select(e => e.Message))}");

        public static Error CircularDependency(string stepName) => Error.InternalServerError(
            Codes.CircularDependency, $"Circular dependency detected involving step '{stepName}'.");
    }

    public static class Codes
    {
        public const string MigratorsMissing      = "Database.Initialization.MigratorsMissing";
        public const string SchemaResetterMissing = "Database.Initialization.SchemaResetterMissing";
        public const string SchemaResetRejected   = "Database.Initialization.SchemaResetRejected";
        public const string MigrationFailed       = "Database.Initialization.MigrationFailed";
        public const string SeedingFailed         = "Database.Initialization.SeedingFailed";
        public const string Unexpected            = "Database.Initialization.Unexpected";
        public const string NoStepsRegistered     = "Database.Initialization.NoStepsRegistered";
        public const string DuplicateStepName     = "Database.Initialization.DuplicateStepName";
        public const string MissingDependency     = "Database.Initialization.MissingDependency";
        public const string StepFailed            = "Database.Initialization.StepFailed";
        public const string CircularDependency    = "Database.Initialization.CircularDependency";
    }
}