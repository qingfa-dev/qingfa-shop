// Assembly marker for Testing library
namespace Testing;

/// <summary>
/// Marker type used to anchor the Testing assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Web.Api assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly WebApiAssembly = typeof(global::Web.Api.AssemblyMarker).Assembly;
}
