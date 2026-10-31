namespace Application.BuildingBlocks.Persistence;

/// <summary>Shared constants for the persistence layer.</summary>
/// <remarks>
/// This class contains cross-cutting constants used across multiple persistence sub-components.
/// Area-specific constants live in their respective folders (Migrations, Seeders, UnitOfWorks, Repositories, Capabilities).
/// </remarks>
public static class PersistenceConstant
{
    /// <summary>Database schema settings.</summary>
    public static class Schema
    {
        /// <summary>The schema owned by ASP.NET Core Identity.</summary>
        public const string Identity = "identity";

        /// <summary>The schema owned by the messaging outbox and inbox.</summary>
        public const string Messaging = "messaging";

        /// <summary>
        /// Platform schemas, in creation order. Module schemas are contributed through
        /// <see cref="IPersistenceSchemaCatalog"/> so reset and initialization cover them all.
        /// </summary>
        public static readonly IReadOnlyList<string> Names = [Identity, Messaging];

        /// <summary>The default schema used when a model does not set one.</summary>
        public const string Default = "public";
    }
}