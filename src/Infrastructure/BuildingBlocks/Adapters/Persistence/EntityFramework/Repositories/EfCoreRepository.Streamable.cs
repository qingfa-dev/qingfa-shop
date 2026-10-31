using Application.BuildingBlocks.Persistence.Capabilities;
using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

/// <summary>Streaming reader using EF Core AsAsyncEnumerable.</summary>
public partial class EfCoreRepository<TEntity, TKey> : IStreamableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    /// <remarks>
    /// Stream: AsAsyncEnumerable provides deferred execution with forward-only streaming,
    /// keeping memory usage minimal for large result sets. Entities are materialized one at a
    /// time as the caller iterates, rather than loading the entire result into memory.
    /// </remarks>
    public async Task<Result<IAsyncEnumerable<TEntity>>> StreamAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: streaming must be supported by the provider.
            var capResult = QueryCapabilityGuard.ValidateCapabilitySupported(
                "Streaming", Capabilities.SupportsStreaming);
            if (capResult.IsFailure)
                return Result<IAsyncEnumerable<TEntity>>.Fail(capResult.Errors);

            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<IAsyncEnumerable<TEntity>>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;
            var stream = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .AsAsyncEnumerable<TEntity>(queryable);

            return Result<IAsyncEnumerable<TEntity>>.Success(stream);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<IAsyncEnumerable<TEntity>>.Fail(TranslateException(ex));
        }
    }
}
