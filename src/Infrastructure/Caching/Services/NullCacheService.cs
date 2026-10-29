using Application.Common.Caching;

namespace Infrastructure.Caching.Services;

/// <summary>Cache facade used when caching is disabled: it always takes the factory path.</summary>
internal sealed class NullCacheService : ICacheService
{
    /// <inheritdoc />
    public async ValueTask<Result<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Error? validation =
            CachingGuard.Key(key) ??
            CachingGuard.Factory(factory) ??
            CachingGuard.Options(options) ??
            CachingGuard.Tags(tags?.ToArray());

        if (validation is not null)
            return Result<T>.Fail(validation);

        return Result<T>.Ok(await factory(cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public ValueTask<Result> SetAsync<T>(
        string key,
        T value,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Error? validation =
            CachingGuard.Key(key) ??
            CachingGuard.Value(value) ??
            CachingGuard.Options(options) ??
            CachingGuard.Tags(tags?.ToArray());

        return ValueTask.FromResult(validation is null
            ? Result.Ok()
            : Result.Fail(validation));
    }

    /// <inheritdoc />
    public ValueTask<Result> RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        Error? validation = CachingGuard.Key(key);

        return ValueTask.FromResult(validation is null
            ? Result.Ok()
            : Result.Fail(validation));
    }

    /// <inheritdoc />
    public ValueTask<Result> RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        Error? validation = CachingGuard.TagsRequired(tags?.ToArray());

        return ValueTask.FromResult(validation is null
            ? Result.Ok()
            : Result.Fail(validation));
    }

    /// <inheritdoc />
    public ValueTask<Result> RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default)
    {
        Error? validation = CachingGuard.Tag(tag);

        return ValueTask.FromResult(validation is null
            ? Result.Ok()
            : Result.Fail(validation));
    }
}
