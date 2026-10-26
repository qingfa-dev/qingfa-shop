// Assembly marker for AppHost.Integration.Tests
namespace AppHost.Integration.Tests;

/// <summary>
/// Marker type used to anchor the AppHost.Integration.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the AppHost assembly is referenced via the SDK-generated Projects type.</summary>
    public static readonly System.Reflection.Assembly AppHostAssembly = typeof(Projects.AppHost).Assembly;
}
