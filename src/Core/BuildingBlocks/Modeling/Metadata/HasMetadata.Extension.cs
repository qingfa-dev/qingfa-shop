namespace Core.BuildingBlocks.Modeling.Metadata;

/// <summary>
/// Extension methods for objects implementing
/// <see cref="IHasMetadata{TMetadata}"/>.
/// </summary>
/// <remarks>
/// Provides convenient typed access to metadata on domain objects,
/// errors, and results without requiring explicit casting.
/// </remarks>
public static class HasMetadataExtensions
{
    #region Raw Access

    /// <summary>
    /// Returns the raw metadata value associated with
    /// <paramref name="metadataKey"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The raw value, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static object? GetValueOrDefault<TMetadata>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault(metadataKey);
    }

    #endregion

    #region Typed Access

    /// <summary>
    /// Returns the converted metadata value associated with
    /// <paramref name="metadataKey"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The converted value, or <c>default</c> if not found or conversion fails.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static TValue? GetValueOrDefault<TMetadata, TValue>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<TValue>(metadataKey);
    }

    /// <summary>
    /// Returns the converted metadata value or
    /// <paramref name="fallback"/> when unavailable.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The converted value, or <paramref name="fallback"/> if not found or conversion fails.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static TValue? GetValueOrDefault<TMetadata, TValue>(
        this IHasMetadata<TMetadata> source,
        string metadataKey,
        TValue? fallback)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault(
            metadataKey,
            fallback);
    }

    #endregion

    #region Required Access

    /// <summary>
    /// Returns the required raw metadata value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The raw value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The metadata key does not exist.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static object GetRequiredValue<TMetadata>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetRequiredValue(metadataKey);
    }

    /// <summary>
    /// Returns the required converted metadata value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The metadata key does not exist.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// The metadata value cannot be converted to <typeparamref name="TValue"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static TValue GetRequiredValue<TMetadata, TValue>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetRequiredValue<TValue>(
            metadataKey);
    }

    #endregion

    #region Try Get

    /// <summary>
    /// Attempts to retrieve and convert a metadata value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The converted value if successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the key exists and conversion succeeds; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static bool TryGetValue<TMetadata, TValue>(
        this IHasMetadata<TMetadata> source,
        string metadataKey,
        out TValue? value)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.TryGetValue(
            metadataKey,
            out value);
    }

    #endregion

    #region Presence

    /// <summary>
    /// Determines whether the metadata contains
    /// <paramref name="metadataKey"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static bool Contains<TMetadata>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.Contains(metadataKey);
    }

    #endregion

    #region Mutation

    /// <summary>
    /// Adds or replaces a metadata value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="metadataKey"/>, or <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public static void SetValue<TMetadata>(
        this IHasMetadata<TMetadata> source,
        string metadataKey,
        object value)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Metadata.SetValue(
            metadataKey,
            value);
    }

    /// <summary>
    /// Removes a metadata value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadataKey">The metadata key.</param>
    /// <returns>
    /// <c>true</c> when the value was found and removed; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="metadataKey"/> is <c>null</c>.
    /// </exception>
    public static bool Remove<TMetadata>(
        this IHasMetadata<TMetadata> source,
        string metadataKey)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.Remove(metadataKey);
    }

    /// <summary>
    /// Merges <paramref name="metadata"/> entries into the carrier's
    /// metadata.
    /// </summary>
    /// <remarks>
    /// Entries from <paramref name="metadata"/> overwrite entries already
    /// present in the carrier's metadata (case-insensitive keys). The merge
    /// mutates the carrier's metadata in place and returns it.
    /// </remarks>
    /// <typeparam name="TSource">The incoming metadata type.</typeparam>
    /// <typeparam name="TMetadata">The carrier metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="metadata">The metadata dictionary supplying the entries.</param>
    /// <returns>The carrier's metadata instance, for chaining.</returns>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="metadata"/> is <c>null</c>.
    /// </exception>
    public static TMetadata MergeMetadata<TSource, TMetadata>(
        this IHasMetadata<TMetadata> source,
        TSource metadata)
        where TSource : IMetadata
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(metadata);

        return source.Metadata.MergeMetadata(metadata);
    }

    #endregion

    #region Well-Known Metadata

    /// <summary>
    /// Gets the correlation ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The correlation ID, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static Guid? GetCorrelationId<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<Guid?>(
            MetadataConstant.Keyword.CorrelationId);
    }

    /// <summary>
    /// Gets the request ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The request ID, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static Guid? GetRequestId<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<Guid?>(
            MetadataConstant.Keyword.RequestId);
    }


    /// <summary>
    /// Gets the event ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The event ID, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static Guid? GetEventId<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<Guid?>(
            MetadataConstant.Keyword.EventId);
    }

    /// <summary>
    /// Gets the causation ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The causation ID, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static Guid? GetCausationId<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<Guid?>(
            MetadataConstant.Keyword.CausationId);
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The timestamp, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static DateTimeOffset? GetTimestamp<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<DateTimeOffset?>(
            MetadataConstant.Keyword.Timestamp);
    }

    /// <summary>
    /// Gets the tenant ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <returns>The tenant ID, or <c>null</c> if not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static Guid? GetTenantId<TMetadata>(
        this IHasMetadata<TMetadata> source)
        where TMetadata : IMetadata
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Metadata.GetValueOrDefault<Guid?>(
            MetadataConstant.Keyword.TenantId);
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetCorrelationId<TMetadata>(
        this IHasMetadata<TMetadata> source,
        Guid? correlationId = null)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.CorrelationId,
            correlationId ?? Guid.NewGuid());

    /// <summary>
    /// Sets the request ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="requestId">The request ID.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetRequestId<TMetadata>(
        this IHasMetadata<TMetadata> source,
        Guid? requestId = null)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.RequestId,
            requestId ?? Guid.NewGuid());

    /// <summary>
    /// Sets the event ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="eventId">The event ID.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetEventId<TMetadata>(
        this IHasMetadata<TMetadata> source,
        Guid? eventId = null)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.EventId,
            eventId ?? Guid.NewGuid());

    /// <summary>
    /// Sets the causation ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="causationId">The causation ID.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetCausationId<TMetadata>(
        this IHasMetadata<TMetadata> source,
        Guid causationId)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.CausationId,
            causationId);

    /// <summary>
    /// Sets the timestamp.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetTimestamp<TMetadata>(
        this IHasMetadata<TMetadata> source,
        DateTimeOffset? timestamp = null)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.Timestamp,
            timestamp ?? DateTimeOffset.UtcNow);

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <param name="source">The metadata carrier.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <exception cref="NotSupportedException">
    /// The underlying metadata implementation does not support mutation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static void SetTenantId<TMetadata>(
        this IHasMetadata<TMetadata> source,
        Guid tenantId)
        where TMetadata : IMetadata
        => source.SetValue(
            MetadataConstant.Keyword.TenantId,
            tenantId);

    #endregion
}