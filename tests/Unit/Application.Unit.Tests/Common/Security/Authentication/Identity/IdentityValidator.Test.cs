using Application.Common.Security.Authentication.Identity;

namespace Application.Unit.Tests.Common.Security.Authentication.Identity;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Validation")]
public sealed class IdentityValidatorTest
{
    private const string Password = "StrongPass1!ab";

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Registration should accept a valid request")]
    public void Register_ShouldAcceptValidRequest()
    {
        IdentityValidator.Validate(
            new RegisterRequest("alice", "alice@example.com", Password, "Alice", "Smith"))
            .IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Registration should reject invalid fields")]
    [InlineData("ab", "alice@example.com", Password, "Identity.Operation.Failed")]
    [InlineData("alice", "bad", Password, "Identity.Credentials.Invalid")]
    [InlineData("alice", "alice@example.com", "short", "Identity.Password.Invalid")]
    public void Register_ShouldRejectInvalidFields(
        string userName,
        string email,
        string password,
        string expectedCode)
    {
        ShouldFailWith(
            IdentityValidator.Validate(new RegisterRequest(userName, email, password)),
            expectedCode);
    }

    [Theory(DisplayName = "Login should require identifier and password")]
    [InlineData("", "pass")]
    [InlineData("alice", "")]
    public void Login_ShouldRequireCredentials(string identifier, string password)
    {
        ShouldFailWith(
            IdentityValidator.Validate(new LoginRequest(identifier, password)),
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "Refresh should require a token")]
    public void Refresh_ShouldRequireToken()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new RefreshRequest(" ")),
            "Identity.RefreshToken.Required");
    }

    [Fact(DisplayName = "Confirmation should require a valid user id and token")]
    public void Confirmation_ShouldValidate()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new ConfirmEmailRequest("bad", "token")),
            "Identity.Token.Invalid");
    }

    [Fact(DisplayName = "Password reset should validate email and password")]
    public void Reset_ShouldValidate()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new ResetPasswordRequest("bad", "token", Password)),
            "Identity.Credentials.Invalid");

        ShouldFailWith(
            IdentityValidator.Validate(new ResetPasswordRequest("alice@example.com", "token", "short")),
            "Identity.Password.Invalid");
    }

    [Fact(DisplayName = "Profile update should require the old password when changing it")]
    public void UpdateInfo_ShouldRequireOldPassword()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new UpdateInfoRequest(NewPassword: Password)),
            "Identity.Password.CurrentRequired");
    }

    [Fact(DisplayName = "Two-factor enablement should require a code")]
    public void TwoFactor_ShouldRequireCode()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new TwoFactorRequest(Enable: true)),
            "Identity.TwoFactor.Required");
    }

    [Fact(DisplayName = "External identities should require provider and key")]
    public void External_ShouldRequireProviderAndKey()
    {
        ShouldFailWith(
            IdentityValidator.Validate(new ExternalUserInfo("", "key", null, null)),
            "Identity.External.LoginFailed");

        ShouldFailWith(
            IdentityValidator.Validate(new ExternalUserInfo("Google", "", null, null)),
            "Identity.External.LoginFailed");
    }

    [Fact(DisplayName = "Validation helpers should accept canonical values")]
    public void Helpers_ShouldAcceptCanonicalValues()
    {
        IdentityValidator.ValidateUserName("alice").IsSuccess.Should().BeTrue();
        IdentityValidator.ValidateEmail("alice@example.com").IsSuccess.Should().BeTrue();
        IdentityValidator.ValidatePassword(Password).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validation helpers should reject malformed values")]
    public void Helpers_ShouldRejectMalformedValues()
    {
        IdentityValidator.ValidateUserName("ab").IsFailure.Should().BeTrue();
        IdentityValidator.ValidateEmail("bad").IsFailure.Should().BeTrue();
        IdentityValidator.ValidatePassword("short").IsFailure.Should().BeTrue();
    }
}
