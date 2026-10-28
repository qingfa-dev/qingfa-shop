namespace Application.Common.Persistence;

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
        /// <summary>The schema owned by the Todo module.</summary>
        public const string Todo = "todo";

        /// <summary>
        /// Every schema the persistence layer owns, in creation order.
        /// Modules append their schema here so reset and initialization cover them all.
        /// </summary>
        public static readonly IReadOnlyList<string> Names = [Todo];

        /// <summary>The default schema used when a model does not set one.</summary>
        public const string Default = Todo;
    }
}