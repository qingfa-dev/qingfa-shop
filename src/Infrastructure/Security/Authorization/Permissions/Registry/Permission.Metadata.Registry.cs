using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Registry;

namespace Infrastructure.Security.Authorization.Permissions.Registry;

/// <summary>Concurrency-safe permission metadata catalog that merges built-ins with persisted claims.</summary>
// Invariant: readers observe a fully built immutable snapshot; writers serialize and swap atomically.
// Boundary: Registry → Store — creates a scope per refresh so the singleton never captures a DbContext.
public sealed partial class PermissionMetadataRegistry(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<PermissionMetadataRegistry> logger) : IPermissionMetadataRegistry
{
    private readonly SemaphoreSlim _refreshLock = new(initialCount: 1, maxCount: 1);
    private PermissionMetadataSnapshot _snapshot =
        PermissionMetadataSnapshot.Create(PermissionContext.All, DateTimeOffset.MinValue);

    /// <inheritdoc />
    public PermissionMetadataSnapshot Snapshot => Volatile.Read(ref _snapshot);

    /// <inheritdoc />
    public PermissionMetadata? Find(string? identifier) => Snapshot.Find(identifier);

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> ByDomain(string? domain) => Snapshot.ByDomain(domain);

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> ByCategory(string? category) => Snapshot.ByCategory(category);

    /// <inheritdoc />
    public async Task<Result<PermissionMetadataSnapshot>> RefreshAsync(
        CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IPermissionStore store = scope.ServiceProvider.GetRequiredService<IPermissionStore>();

            // Call: runtime claims are the only source of identifiers that are not compiled in.
            Result<HashSet<string>> identifiers = await store
                .GetAllPermissionIdentifiersAsync(ct)
                .ConfigureAwait(false);

            if (identifiers.IsFailure)
                return Result<PermissionMetadataSnapshot>.Fail(identifiers.Errors);

            var merged = new Dictionary<string, PermissionMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (PermissionMetadata permission in PermissionContext.All)
                merged[permission.Identifier] = permission;

            int added = 0;
            foreach (string identifier in identifiers.Value.Order(StringComparer.OrdinalIgnoreCase))
            {
                if (merged.ContainsKey(identifier))
                    continue;

                // Validate: only canonical identifiers can become metadata; skip legacy claim values.
                Result<PermissionMetadata> parsed = PermissionMetadata.TryParse(identifier);
                if (parsed.IsFailure)
                {
                    Loggers.LogSkippedIdentifier(logger, identifier);
                    continue;
                }

                merged[identifier] = parsed.Value;
                added++;
            }

            // Publish: build the immutable snapshot first, then swap the reference in one write.
            PermissionMetadataSnapshot next = PermissionMetadataSnapshot.Create(
                merged.Values,
                clock.GetUtcNow());

            Volatile.Write(ref _snapshot, next);
            Loggers.LogRefreshed(logger, next.Count, added, next.RefreshedAtUtc);
            return Result<PermissionMetadataSnapshot>.Ok(next);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Loggers.LogRefreshFailed(logger, ex);
            return Result<PermissionMetadataSnapshot>.Fail(
                PermissionRegistryOutcome.Failure.RefreshFailed(ex));
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
