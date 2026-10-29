using Application.Common.Security.Authentication.Identity;

namespace Application.Unit.Tests.Common.Security.Authentication.Identity;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Outcomes")]
public sealed class IdentityOutcomeTest
{
    private static Error[] Failures =>
    [
        IdentityOutcome.Failure.UserNotFound,
        IdentityOutcome.Failure.UserRequired,
        IdentityOutcome.Failure.UserNameTaken,
        IdentityOutcome.Failure.EmailTaken,
        IdentityOutcome.Failure.InvalidCredentials,
        IdentityOutcome.Failure.Inactive,
        IdentityOutcome.Failure.LockedOut,
        IdentityOutcome.Failure.NotAllowed,
        IdentityOutcome.Failure.EmailNotConfirmed,
        IdentityOutcome.Failure.InvalidToken,
        IdentityOutcome.Failure.RefreshTokenRequired,
        IdentityOutcome.Failure.CurrentPasswordRequired,
        IdentityOutcome.Failure.CurrentPasswordInvalid,
        IdentityOutcome.Failure.PasswordInvalid("reason"),
        IdentityOutcome.Failure.TwoFactorRequired,
        IdentityOutcome.Failure.TwoFactorInvalid,
        IdentityOutcome.Failure.TwoFactorNotEnabled,
        IdentityOutcome.Failure.ExternalProviderUnsupported("google"),
        IdentityOutcome.Failure.ExternalLoginFailed,
        IdentityOutcome.Failure.EmailDeliveryFailed,
        IdentityOutcome.Failure.OperationFailed("reason")
    ];

    [Fact(DisplayName = "Identity outcomes should be unique and namespaced")]
    public void Outcomes_ShouldBeUniqueAndNamespaced()
    {
        Error[] failures = Failures;

        failures.Should().OnlyHaveUniqueItems(error => error.Code);
        failures.Should().OnlyContain(error =>
            error.Code.StartsWith("Identity.", StringComparison.Ordinal));
        failures.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact(DisplayName = "Identity outcomes should preserve HTTP semantics")]
    public void Outcomes_ShouldPreserveHttpSemantics()
    {
        IdentityOutcome.Failure.UserNotFound.Status.Should().Be(404);
        IdentityOutcome.Failure.UserNameTaken.Status.Should().Be(409);
        IdentityOutcome.Failure.InvalidCredentials.Status.Should().Be(401);
        IdentityOutcome.Failure.Inactive.Status.Should().Be(403);
        IdentityOutcome.Failure.InvalidToken.Status.Should().Be(400);
        IdentityOutcome.Failure.EmailDeliveryFailed.Status.Should().Be(500);
    }

    [Fact(DisplayName = "Unknown Identity errors should map to the generic operation failure")]
    public void UnknownIdentityError_ShouldMapToOperationFailed()
    {
        IdentityOutcome.Failure.IdentityError("Unknown", "unknown").Code
            .Should().Be("Identity.Operation.Failed");
    }

    [Fact(DisplayName = "Identity errors should map common Identity codes")]
    public void IdentityErrors_ShouldMapCommonCodes()
    {
        IdentityOutcome.Failure.IdentityError("DuplicateUserName", "taken").Code
            .Should().Be("Identity.UserName.Taken");
        IdentityOutcome.Failure.IdentityError("DuplicateEmail", "taken").Code
            .Should().Be("Identity.Email.Taken");
        IdentityOutcome.Failure.IdentityError("PasswordTooShort", "short").Code
            .Should().Be("Identity.Password.TooShort");
        IdentityOutcome.Failure.IdentityError("Unknown", "unknown").Code
            .Should().Be("Identity.Operation.Failed");
    }
}
