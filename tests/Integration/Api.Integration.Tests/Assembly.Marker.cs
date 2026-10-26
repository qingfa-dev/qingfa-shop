// Assembly marker for Api.Integration.Tests
namespace Api.Integration.Tests;

/// <summary>
/// Marker type used to anchor the Api.Integration.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Web.Api assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly WebApiAssembly = typeof(global::Web.Api.AssemblyMarker).Assembly;
}
