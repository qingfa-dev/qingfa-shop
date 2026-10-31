using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Users")]
public sealed class UserOutcomeTest
{
    private static Error[] Failures =>
    [
        UserOutcome.Failure.EntityRequired,
        UserOutcome.Failure.IdRequired,
        UserOutcome.Failure.NotFound,
        UserOutcome.Failure.CredentialRequired,
        UserOutcome.Failure.CredentialInvalid,
        UserOutcome.Failure.Unauthorized,
        UserOutcome.Failure.UsernameRequired,
        UserOutcome.Failure.UsernameTooShort,
        UserOutcome.Failure.UsernameTooLong,
        UserOutcome.Failure.UsernameInvalid,
        UserOutcome.Failure.UsernameDuplicate,
        UserOutcome.Failure.EmailRequired,
        UserOutcome.Failure.EmailTooLong,
        UserOutcome.Failure.EmailInvalid,
        UserOutcome.Failure.EmailDuplicate,
        UserOutcome.Failure.EmailSameAsCurrent,
        UserOutcome.Failure.EmailPendingAlready,
        UserOutcome.Failure.EmailNoPendingChange,
        UserOutcome.Failure.FirstNameRequired,
        UserOutcome.Failure.FirstNameTooLong,
        UserOutcome.Failure.LastNameRequired,
        UserOutcome.Failure.LastNameTooLong,
        UserOutcome.Failure.PasswordRequired,
        UserOutcome.Failure.PasswordTooShort,
        UserOutcome.Failure.PasswordTooLong,
        UserOutcome.Failure.PasswordTooWeak,
        UserOutcome.Failure.PasswordMismatch,
        UserOutcome.Failure.InvalidCredentials,
        UserOutcome.Failure.PasswordNoPendingReset,
        UserOutcome.Failure.CurrentPasswordRequired,
        UserOutcome.Failure.Inactive,
        UserOutcome.Failure.AlreadyDeactivated,
        UserOutcome.Failure.AlreadyActive,
        UserOutcome.Failure.InvalidToken,
        UserOutcome.Failure.TokenExpired,
        UserOutcome.Failure.TokenRequired,
        UserOutcome.Failure.PhoneInvalid,
        UserOutcome.Failure.PhoneTooLong,
        UserOutcome.Failure.PhoneRequired,
        UserOutcome.Failure.PhoneSameAsCurrent,
        UserOutcome.Failure.PhoneDuplicate,
        UserOutcome.Failure.PhonePendingAlready,
        UserOutcome.Failure.PhoneNoPendingChange,
        UserOutcome.Failure.PhoneOtpAlreadySent,
        UserOutcome.Failure.OtpRequired,
        UserOutcome.Failure.OtpTooShort,
        UserOutcome.Failure.OtpTooLong,
        UserOutcome.Failure.OtpInvalid,
        UserOutcome.Failure.DateOfBirthFuture,
        UserOutcome.Failure.DateOfBirthUnderage,
        UserOutcome.Failure.DateOfBirthTooOld,
        UserOutcome.Failure.ExternalLoginProviderError,
        UserOutcome.Failure.ExternalLoginEmailMissing,
        UserOutcome.Failure.ExternalLoginUnsupportedProvider,
        UserOutcome.Failure.ExternalLoginTokenInvalid,
        UserOutcome.Failure.ProfileCreationFailed,
        UserOutcome.Failure.AssignDenied("permission"),
        UserOutcome.Failure.RevokeDenied("permission"),
        UserOutcome.Failure.SelfStatusToggle,
        UserOutcome.Failure.SelfDelete
    ];

    [Fact(DisplayName = "User outcomes should be unique, namespaced, and descriptive")]
    public void Outcomes_ShouldBeUniqueAndNamespaced()
    {
        Error[] failures = Failures;

        failures.Should().OnlyHaveUniqueItems(error => error.Code);
        failures.Should().OnlyContain(error => error.Code.StartsWith("User.", StringComparison.Ordinal));
        failures.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact(DisplayName = "User outcomes should expose expected HTTP semantics")]
    public void Outcomes_ShouldExposeHttpSemantics()
    {
        UserOutcome.Failure.EntityRequired.Status.Should().Be(422);
        UserOutcome.Failure.NotFound.Status.Should().Be(404);
        UserOutcome.Failure.UsernameDuplicate.Status.Should().Be(409);
        UserOutcome.Failure.Inactive.Status.Should().Be(403);
        UserOutcome.Failure.InvalidCredentials.Status.Should().Be(401);
        UserOutcome.Failure.ProfileCreationFailed.Status.Should().Be(500);
    }
}
