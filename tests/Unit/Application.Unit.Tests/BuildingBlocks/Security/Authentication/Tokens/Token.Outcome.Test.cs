using Application.BuildingBlocks.Security.Authentication.Tokens;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Tokens;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class TokenOutcomeTest
{
    private static Error[] Failures =>
    [
        TokenOutcome.Failure.SigningKeyRequired,
        TokenOutcome.Failure.SigningKeyTooShort,
        TokenOutcome.Failure.IssuerRequired,
        TokenOutcome.Failure.AudienceRequired,
        TokenOutcome.Failure.AccessTokenLifetimeInvalid,
        TokenOutcome.Failure.ClockSkewInvalid,
        TokenOutcome.Failure.RefreshTokenLifetimeInvalid,
        TokenOutcome.Failure.RefreshTokenLengthInvalid,
        TokenOutcome.Failure.RefreshTokenPrefixRequired,
        TokenOutcome.Failure.BlacklistPrefixRequired,
        TokenOutcome.Failure.UserIdRequired,
        TokenOutcome.Failure.UserIdTooLong,
        TokenOutcome.Failure.UserNameTooLong,
        TokenOutcome.Failure.EmailTooLong,
        TokenOutcome.Failure.ClaimKeyRequired,
        TokenOutcome.Failure.ClaimKeyTooLong,
        TokenOutcome.Failure.ClaimKeyReserved,
        TokenOutcome.Failure.RefreshTokenRequired,
        TokenOutcome.Failure.RefreshTokenTooLong,
        TokenOutcome.Failure.JtiRequired,
        TokenOutcome.Failure.JtiTooLong,
        TokenOutcome.Failure.BlacklistExpiryRequired,
        TokenOutcome.Failure.AccessTokenGenerationFailed,
        TokenOutcome.Failure.RefreshTokenGenerationFailed,
        TokenOutcome.Failure.BlacklistStoreFailure,
        TokenOutcome.Failure.RotationFailed
    ];

    [Fact(DisplayName = "Token failure codes should be unique and namespaced")]
    public void FailureCodes_ShouldBeUniqueAndNamespaced()
    {
        Error[] failures = Failures;

        failures.Should().OnlyHaveUniqueItems(error => error.Code);
        failures.Should().OnlyContain(error => error.Code.StartsWith("Token.", StringComparison.Ordinal));
        failures.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact(DisplayName = "Request failures should be bad requests")]
    public void RequestFailures_ShouldBeBadRequests()
    {
        Failures.Where(error => error.Status == 400).Should().HaveCount(12);
    }

    [Fact(DisplayName = "Configuration failures should be validation errors")]
    public void ConfigurationFailures_ShouldBeValidationErrors()
    {
        Failures.Where(error => error.Status == 422).Should().HaveCount(10);
    }

    [Fact(DisplayName = "Service failures should be unexpected")]
    public void ServiceFailures_ShouldBeUnexpected()
    {
        Failures.Where(error => error.Status == 500).Should().HaveCount(4);
    }
}
