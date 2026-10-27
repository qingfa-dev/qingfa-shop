using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Application.Common.Querying.Mapping;

/// <summary>The compose-time allowlist of query maps, keyed by the type each map describes.</summary>
/// <remarks>
/// Maps are registered once while the application composes and are read concurrently
/// afterwards. A registration is atomic and a second map for the same type is
/// rejected rather than replacing the first, so a lookup can never observe a
/// half-composed registry.
/// <para>
/// Lookups are exact by type: a map registered for a base type does not resolve a
/// derived type.
/// </para>
/// </remarks>
public sealed class QueryMapRegistry : IQueryMapProvider
{
    private readonly ConcurrentDictionary<Type, object> _maps = new();

    /// <summary>Registers the map for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type the map describes.</typeparam>
    /// <param name="map">The map to register.</param>
    /// <returns>
    /// Success when the type had no map; otherwise a failure reporting that the type
    /// is already registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="map"/> is <c>null</c>.
    /// </exception>
    public Result Register<T>(QueryMap<T> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return _maps.TryAdd(typeof(T), map)
            ? Result.Success()
            : Result.Fail(QueryMapOutcome.Failure.AlreadyRegistered(typeof(T)));
    }

    /// <summary>Looks up the map for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type whose map is wanted.</typeparam>
    /// <param name="map">The registered map, when one exists.</param>
    /// <returns>True when a map is registered for <typeparamref name="T"/>.</returns>
    public bool TryGet<T>([NotNullWhen(true)] out QueryMap<T>? map)
    {
        if (_maps.TryGetValue(typeof(T), out object? value))
        {
            map = value as QueryMap<T>;
            return map is not null;
        }

        map = null;
        return false;
    }

    /// <inheritdoc cref="IQueryMapProvider.TryGet{T}(out QueryMap{T}?)"/>
    bool IQueryMapProvider.TryGet<T>(out QueryMap<T>? map) => TryGet<T>(out map);

    /// <summary>Looks up the map for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type whose map is wanted.</typeparam>
    /// <returns>The registered map, or a failure when the type has none.</returns>
    public Result<QueryMap<T>> Get<T>() =>
        TryGet<T>(out QueryMap<T>? map)
            ? Result<QueryMap<T>>.Success(map)
            : Result<QueryMap<T>>.Fail(QueryMapOutcome.Failure.NotRegistered(typeof(T)));

    /// <summary>Reports whether a map is registered for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True when a map is registered.</returns>
    public bool Contains<T>() => _maps.ContainsKey(typeof(T));

    /// <inheritdoc cref="IQueryMapProvider.IsRegistered{T}" />
    public bool IsRegistered<T>() where T : class => Contains<T>();

    /// <summary>Gets the number of registered maps.</summary>
    public int Count => _maps.Count;

    /// <summary>Gets the types that have a registered map.</summary>
    public IReadOnlyCollection<Type> RegisteredTypes => _maps.Keys.ToArray();
}
