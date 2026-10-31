using Application.BuildingBlocks.UseCases.Mediation.Commands;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>Architecture tests for shared MediatR command markers.</summary>
public sealed class CommandMarkerArchitectureTests
{
    private static readonly string[] SelfManagedNamespaces =
    [
        "Application.BuildingBlocks.Security.Identity.Auth",
        "Application.BuildingBlocks.Security.Identity.External",
    ];

    [Fact(DisplayName = "Non-transactional commands should only live in self-managed identity slices")]
    public void NonTransactionalCommand_ShouldOnlyBeImplementedBySelfManagedSlices()
    {
        Type[] implementations = typeof(INonTransactionalCommand).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(INonTransactionalCommand).IsAssignableFrom(type))
            .ToArray();

        implementations.Should().NotBeEmpty(
            "the auth slice commands should opt out of the ambient transaction");
        implementations.Should().OnlyContain(type =>
            type.Namespace != null &&
            SelfManagedNamespaces.Any(ns =>
                type.Namespace == ns ||
                type.Namespace.StartsWith(ns + ".", StringComparison.Ordinal)));
    }
}
