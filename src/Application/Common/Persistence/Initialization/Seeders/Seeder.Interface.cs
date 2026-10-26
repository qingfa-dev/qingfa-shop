namespace Application.Common.Persistence.Initialization.Seeders;

public interface IPersistenceSeeder
{
    int Order { get; }

    Task<Result> SeedAsync(
        CancellationToken cancellationToken = default);
}

public interface IPersistenceSeeder<TEntity, TKey> : IPersistenceSeeder
    where TEntity : class
{
}