using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Tokens.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenOutcomeTest
{
    private static Error[] Failures =>
    [
        RefreshTokenOutcome.Failure.EntityRequired,
        RefreshTokenOutcome.Failure.NotFound,
        RefreshTokenOutcome.Failure.Expired,
        RefreshTokenOutcome.Failure.Revoked,
        RefreshTokenOutcome.Failure.TheftDetected,
        RefreshTokenOutcome.Failure.TokenHashRequired,
        RefreshTokenOutcome.Failure.TokenHashTooLong,
        RefreshTokenOutcome.Failure.UserIdRequired,
        RefreshTokenOutcome.Failure.UserIdTooLong,
        RefreshTokenOutcome.Failure.TokenFamilyIdRequired,
        RefreshTokenOutcome.Failure.CreatedAtRequired,
        RefreshTokenOutcome.Failure.ExpiresAtRequired,
        RefreshTokenOutcome.Failure.ExpiresAtBeforeCreated,
        RefreshTokenOutcome.Failure.RevokedAtBeforeCreated,
        RefreshTokenOutcome.Failure.RevocationReasonRequired,
        RefreshTokenOutcome.Failure.RevocationReasonInvalid,
        RefreshTokenOutcome.Failure.RevocationReasonWithoutRevocation,
        RefreshTokenOutcome.Failure.ReplacementRequired,
        RefreshTokenOutcome.Failure.ReplacementSameToken,
        RefreshTokenOutcome.Failure.ReplacementFamilyMismatch,
        RefreshTokenOutcome.Failure.ReplacementUserMismatch,
        RefreshTokenOutcome.Failure.ReplacementNotActive,
        RefreshTokenOutcome.Failure.GenerationFailed,
        RefreshTokenOutcome.Failure.RotationFailed,
        RefreshTokenOutcome.Failure.RevocationFailed,
        RefreshTokenOutcome.Failure.TokenTheftDetectorFailure,
        RefreshTokenOutcome.Failure.StoreFailure
    ];

    [Fact(DisplayName = "Failure codes should be unique and namespaced")]
    public void FailureCodes_ShouldBeUniqueAndNamespaced()
    {
        Error[] failures = Failures;

        failures.Should().OnlyHaveUniqueItems(error => error.Code);
        failures.Should().OnlyContain(error => error.Code.StartsWith("RefreshToken.", StringComparison.Ordinal));
        failures.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact(DisplayName = "Authentication failures should be unauthorized")]
    public void AuthenticationFailures_ShouldBeUnauthorized()
    {
        Error[] failures = Failures;

        failures.Where(error => error.Status == 401)
            .Select(error => error.Code)
            .Should()
            .BeEquivalentTo(
            [
                "RefreshToken.Expired",
                "RefreshToken.Revoked",
                "RefreshToken.TheftDetected"
            ]);
    }

    [Fact(DisplayName = "Validation failures should be bad requests")]
    public void ValidationFailures_ShouldBeBadRequests()
    {
        Error[] failures = Failures;

        failures.Where(error => error.Status == 400)
            .Should()
            .HaveCount(13);
    }

    [Fact(DisplayName = "Infrastructure failures should be unexpected")]
    public void InfrastructureFailures_ShouldBeUnexpected()
    {
        Error[] failures = Failures;

        failures.Where(error => error.Status == 500)
            .Select(error => error.Code)
            .Should()
            .BeEquivalentTo(
            [
                "RefreshToken.GenerationFailed",
                "RefreshToken.RotationFailed",
                "RefreshToken.RevocationFailed",
                "RefreshToken.TokenTheftDetectorFailure",
                "RefreshToken.StoreFailure"
            ]);
    }

    [Fact(DisplayName = "Entity and lookup failures should keep their HTTP semantics")]
    public void EntityFailures_ShouldKeepHttpSemantics()
    {
        RefreshTokenOutcome.Failure.EntityRequired.Status.Should().Be(422);
        RefreshTokenOutcome.Failure.NotFound.Status.Should().Be(404);
        RefreshTokenOutcome.Failure.ReplacementSameToken.Status.Should().Be(409);
        RefreshTokenOutcome.Failure.ReplacementFamilyMismatch.Status.Should().Be(409);
        RefreshTokenOutcome.Failure.ReplacementUserMismatch.Status.Should().Be(409);
        RefreshTokenOutcome.Failure.ReplacementNotActive.Status.Should().Be(409);
    }
}
