using Application.Common.Mediators.Commands;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>Architecture tests for shared MediatR command markers.</summary>
public sealed class CommandMarkerArchitectureTests
{
    private const string AuthSliceNamespace = "Application.Features.Identity.Auth";

    [Fact(DisplayName = "Non-transactional commands should only live in the auth slice")]
    public void NonTransactionalCommand_ShouldOnlyBeImplementedByAuthSlice()
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
            (type.Namespace == AuthSliceNamespace ||
                type.Namespace.StartsWith(AuthSliceNamespace + ".", StringComparison.Ordinal)));
    }
}
