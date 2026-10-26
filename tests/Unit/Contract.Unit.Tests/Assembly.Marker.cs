// Assembly marker for Contract.Unit.Tests
namespace Contract.Unit.Tests;

/// <summary>
/// Marker type used to anchor the Contract.Unit.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Contract layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly ContractAssembly = typeof(Contract.AssemblyMarker).Assembly;
}
