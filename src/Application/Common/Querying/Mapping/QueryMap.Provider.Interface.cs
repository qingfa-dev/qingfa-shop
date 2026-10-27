namespace Application.Common.Querying.Mapping;

/// <summary>
/// Provides access to registered <see cref="QueryMap{T}"/> instances.
/// </summary>
/// <remarks>
/// Consumers resolve this interface from DI instead of depending on the concrete
/// <see cref="QueryMapRegistry"/> type, keeping the Application layer decoupled
/// from the registration implementation.
/// </remarks>
public interface IQueryMapProvider
{
    /// <summary>
    /// Looks up the query map for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type whose map is wanted.</typeparam>
    /// <param name="map">The registered map when one exists; otherwise <c>null</c>.</param>
    /// <returns>
    /// <c>true</c> when a map is registered for <typeparamref name="T"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryGet<T>(out QueryMap<T>? map) where T : class;

    /// <summary>
    /// Reports whether a query map is registered for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type to check.</typeparam>
    /// <returns><c>true</c> when a map is registered; otherwise <c>false</c>.</returns>
    bool IsRegistered<T>() where T : class;
}
