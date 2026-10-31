namespace Application.BuildingBlocks.Querying.Mapping;

/// <summary>
/// Contributes query map registrations to the <see cref="QueryMapRegistry"/>
/// during application composition.
/// </summary>
/// <remarks>
/// Implementations live next to the maps they register and are discovered from
/// the persistence assembly, so adding a new map does not require editing the
/// composition root.
/// </remarks>
public interface IQueryMapDefinition
{
    /// <summary>Registers this definition's maps in the supplied registry.</summary>
    /// <param name="registry">The registry being composed.</param>
    /// <returns>Success when every map was registered; otherwise the registration failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <c>null</c>.</exception>
    Result Register(QueryMapRegistry registry);
}
