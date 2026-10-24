namespace Web.Api;

/// <summary>
/// Anchors the Api assembly so <c>WebApplicationFactory&lt;AssemblyMarker&gt;</c> can
/// boot the real host without referencing a controller or endpoint type.
/// </summary>
public sealed class AssemblyMarker { }