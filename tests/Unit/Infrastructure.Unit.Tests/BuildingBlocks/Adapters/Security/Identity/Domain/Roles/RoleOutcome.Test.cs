using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleOutcomeTest
{
    private static Error[] Failures =>
    [
        RoleOutcome.Failure.EntityRequired,
        RoleOutcome.Failure.NotFound,
        RoleOutcome.Failure.NameRequired,
        RoleOutcome.Failure.NameTooLong,
        RoleOutcome.Failure.DescriptionTooLong,
        RoleOutcome.Failure.AlreadyExists,
        RoleOutcome.Failure.SystemRoleProtected,
        RoleOutcome.Failure.CannotDeleteRoleWithUsers,
        RoleOutcome.Failure.AuthRequired,
        RoleOutcome.Failure.AssignDenied("permission"),
        RoleOutcome.Failure.RevokeDenied("permission")
    ];

    [Fact(DisplayName = "Role outcomes should be unique, namespaced, and descriptive")]
    public void Outcomes_ShouldBeUniqueAndNamespaced()
    {
        Error[] failures = Failures;

        failures.Should().OnlyHaveUniqueItems(error => error.Code);
        failures.Should().OnlyContain(error => error.Code.StartsWith("Role.", StringComparison.Ordinal));
        failures.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact(DisplayName = "Role outcomes should expose expected HTTP semantics")]
    public void Outcomes_ShouldExposeHttpSemantics()
    {
        RoleOutcome.Failure.EntityRequired.Status.Should().Be(422);
        RoleOutcome.Failure.NotFound.Status.Should().Be(404);
        RoleOutcome.Failure.AlreadyExists.Status.Should().Be(409);
        RoleOutcome.Failure.SystemRoleProtected.Status.Should().Be(403);
        RoleOutcome.Failure.AuthRequired.Status.Should().Be(401);
    }
}
