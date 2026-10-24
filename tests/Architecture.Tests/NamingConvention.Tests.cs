using System.Reflection;
using Xunit;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>
/// Naming convention tests enforcing the Dotted File Naming guide.
/// </summary>
/// <remarks>
/// Note: Full NetArchTest rules are documented here but require NetArchTest API compatibility.
/// Current version uses simplified compile-time checks and reflection.
/// </remarks>
public sealed class NamingConventionTests
{
    private static readonly Assembly CoreAssembly = typeof(QingFa.Shop.Core.AssemblyMarker).Assembly;
    private static readonly Assembly ContractAssembly = typeof(QingFa.Shop.Contract.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(QingFa.Shop.Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(QingFa.Shop.Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly ServiceDefaultsAssembly = typeof(QingFa.Shop.ServiceDefaults.AssemblyMarker).Assembly;
    private static readonly Assembly WebApiAssembly = typeof(Web.Api.AssemblyMarker).Assembly;

    private static readonly Assembly[] AllAssemblies =
    [
        CoreAssembly,
        ContractAssembly,
        ApplicationAssembly,
        InfrastructureAssembly,
        ServiceDefaultsAssembly,
        WebApiAssembly
    ];

    // --- C# types follow PascalCase ---
    [Fact]
    public void AllTypes_ShouldBePascalCase()
    {
        var allTypes = AllAssemblies.SelectMany(a => a.GetTypes());
        var nonPascalCase = allTypes
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .Where(t => !IsPascalCase(t.Name))
            .ToList();

        nonPascalCase.Should().BeEmpty(because: "All C# types must be PascalCase per dotted naming guide");
    }

    // --- Interfaces use .Interface suffix ---
    [Fact]
    public void Interfaces_ShouldUseInterfaceSuffix()
    {
        var interfaces = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface)
            .Where(t => !IsBclInterface(t))
            .ToList();

        var nonCompliant = interfaces
            .Where(t => !t.Name.EndsWith("Interface"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Interfaces must use .Interface suffix per C# naming convention (§3.2.1)");
    }

    // --- Abstract base classes use .Base suffix ---
    [Fact]
    public void AbstractClasses_ShouldUseBaseSuffix()
    {
        var abstractClasses = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAbstract && t.IsClass)
            .ToList();

        var nonCompliant = abstractClasses
            .Where(t => !t.Name.EndsWith("Base"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Abstract base classes must use .Base suffix per C# naming convention (§3.2.2)");
    }

    // --- Validators use .Validator suffix ---
    [Fact]
    public void Validators_ShouldUseValidatorSuffix()
    {
        var validators = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Validator"))
            .ToList();

        var nonCompliant = validators
            .Where(t => !t.Name.EndsWith(".Validator"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Validators must use .Validator suffix per naming convention");
    }

    // --- Mappers use .Mapper suffix ---
    [Fact]
    public void Mappers_ShouldUseMapperSuffix()
    {
        var mappers = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Mapper"))
            .ToList();

        var nonCompliant = mappers
            .Where(t => !t.Name.EndsWith(".Mapper"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Mappers must use .Mapper suffix per naming convention");
    }

    // --- Policies use .Policy suffix ---
    [Fact]
    public void Policies_ShouldUsePolicySuffix()
    {
        var policies = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Policy"))
            .ToList();

        var nonCompliant = policies
            .Where(t => !t.Name.EndsWith(".Policy"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Policies must use .Policy suffix per naming convention");
    }

    // --- Specifications use .Specification suffix ---
    [Fact]
    public void Specifications_ShouldUseSpecificationSuffix()
    {
        var specs = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Specification"))
            .ToList();

        var nonCompliant = specs
            .Where(t => !t.Name.EndsWith(".Specification"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Specifications must use .Specification suffix per naming convention");
    }

    // --- Handlers use .Handler suffix ---
    [Fact]
    public void Handlers_ShouldUseHandlerSuffix()
    {
        var handlers = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Handler"))
            .ToList();

        var nonCompliant = handlers
            .Where(t => !t.Name.EndsWith(".Handler"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Handlers must use .Handler suffix per naming convention");
    }

    // --- Factories use .Factory suffix ---
    [Fact]
    public void Factories_ShouldUseFactorySuffix()
    {
        var factories = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Factory"))
            .ToList();

        var nonCompliant = factories
            .Where(t => !t.Name.EndsWith(".Factory"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Factories must use .Factory suffix per naming convention");
    }

    // --- Builders use .Builder suffix ---
    [Fact]
    public void Builders_ShouldUseBuilderSuffix()
    {
        var builders = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Builder"))
            .ToList();

        var nonCompliant = builders
            .Where(t => !t.Name.EndsWith(".Builder"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Builders must use .Builder suffix per naming convention");
    }

    // --- Extensions use .Extension suffix ---
    [Fact]
    public void Extensions_ShouldUseExtensionSuffix()
    {
        var extensions = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Extension"))
            .ToList();

        var nonCompliant = extensions
            .Where(t => !t.Name.EndsWith(".Extension"))
            .ToList();

        nonCompliant.Should().BeEmpty(because: "Extensions must use .Extension suffix per naming convention");
    }

    // --- Assembly markers use AssemblyMarker ---
    [Fact]
    public void AssemblyMarkers_ShouldBeNamedAssemblyMarker()
    {
        var markers = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name == "AssemblyMarker")
            .ToList();

        markers.Should().HaveCount(AllAssemblies.Length, because: "Each assembly must have exactly one AssemblyMarker");
    }

    // --- No "Helper", "Util", "Manager", "Service" as standalone names ---
    [Fact]
    public void Types_ShouldNotUseVagueNames()
    {
        var vagueNames = new[] { "Helper", "Util", "Manager", "Service", "Provider", "Processor", "Handler" };

        var allTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var vagueTypes = allTypes
            .Where(t => vagueNames.Contains(t.Name))
            .ToList();

        vagueTypes.Should().BeEmpty(because: "Type names must be specific (e.g., OrderService not Service) per naming guide anti-patterns");
    }

    // --- No "Impl" suffix ---
    [Fact]
    public void Implementations_ShouldNotUseImplSuffix()
    {
        var allTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Impl"))
            .ToList();

        allTypes.Should().BeEmpty(because: "Use .Default, .Base, or concrete name instead of Impl suffix");
    }

    private static bool IsPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsUpper(name[0])) return false;
        // Allow dots for nested types, but each segment should be PascalCase
        return name.Split('.').All(s => char.IsUpper(s[0]) && s.All(c => char.IsLetterOrDigit(c)));
    }

    private static bool IsBclInterface(Type type)
    {
        var bclInterfaces = new[]
        {
            "IDisposable", "IAsyncDisposable", "ICloneable", "IComparable",
            "IEquatable`1", "IEnumerable`1", "IEnumerator`1", "ICollection`1",
            "IList`1", "IReadOnlyCollection`1", "IReadOnlyList`1"
        };
        return bclInterfaces.Any(b => type.Name == b || type.Name.StartsWith(b + "`"));
    }
}