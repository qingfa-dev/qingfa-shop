namespace Application.BuildingBlocks.Persistence.Initialization.Seeders;
/// <summary>
/// Specialized step for database seeding.
/// Inherits common metadata and execution from <see cref="IInitializationStep"/>.
/// </summary>
public interface IPersistenceSeeder : IInitializationStep
{
    /// <summary>
    /// Gets the entity type this seeder operates on (for generic seeders).
    /// Null for non-generic/raw seeders.
    /// </summary>
    Type? EntityType { get; }

    /// <summary>
    /// Gets the key type for the entity (for generic seeders).
    /// </summary>
    Type? KeyType { get; }
}

/// <summary>
/// Generic seeder for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type being seeded.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
public interface IPersistenceSeeder<TEntity, TKey> : IPersistenceSeeder
    where TEntity : class
{
    /// <inheritdoc />
    new Type? EntityType => typeof(TEntity);

    /// <inheritdoc />
    new Type? KeyType => typeof(TKey);
}