using System.Reflection;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>Enforces the BuildingBlocks/Modules dependency direction.</summary>
public sealed class BuildingBlockBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(Core.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.AssemblyMarker).Assembly;

    [Fact(DisplayName = "Core BuildingBlocks should not reference Core modules")]
    public void CoreBuildingBlocks_ShouldNotReferenceCoreModules()
    {
        AssertNoReferences(
            CoreAssembly,
            "Core.BuildingBlocks.",
            "Core.Modules.");
    }

    [Fact(DisplayName = "Application BuildingBlocks should not reference application or Core modules")]
    public void ApplicationBuildingBlocks_ShouldNotReferenceApplicationModules()
    {
        AssertNoReferences(
            ApplicationAssembly,
            "Application.BuildingBlocks.",
            "Application.Modules.");

        AssertNoReferences(
            ApplicationAssembly,
            "Application.BuildingBlocks.",
            "Core.Modules.");
    }

    [Fact(DisplayName = "Infrastructure BuildingBlocks should not reference any module")]
    public void InfrastructureBuildingBlocks_ShouldNotReferenceModules()
    {
        AssertNoReferences(
            InfrastructureAssembly,
            "Infrastructure.BuildingBlocks.",
            "Infrastructure.Modules.");

        AssertNoReferences(
            InfrastructureAssembly,
            "Infrastructure.BuildingBlocks.",
            "Application.Modules.");

        AssertNoReferences(
            InfrastructureAssembly,
            "Infrastructure.BuildingBlocks.",
            "Core.Modules.");
    }

    private static void AssertNoReferences(
        Assembly assembly,
        string sourcePrefix,
        string forbiddenPrefix)
    {
        var sourceTypes = assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(sourcePrefix, StringComparison.Ordinal) == true)
            .ToList();

        sourceTypes.Should().NotBeEmpty(
            because: $"{sourcePrefix} must contain scanned types; a namespace drift would make this guard vacuous");

        var offenders = sourceTypes
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => referenced.Namespace?.StartsWith(
                    forbiddenPrefix,
                    StringComparison.Ordinal) == true)
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            because: $"{sourcePrefix} must not depend on {forbiddenPrefix}");
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        if (type.BaseType is not null)
            yield return type.BaseType;

        foreach (Type contract in type.GetInterfaces())
            yield return contract;

        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return field.FieldType;
        }

        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return property.PropertyType;
        }

        foreach (MethodInfo method in type.GetMethods(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (ParameterInfo parameter in method.GetParameters())
                yield return parameter.ParameterType;
        }
    }
}