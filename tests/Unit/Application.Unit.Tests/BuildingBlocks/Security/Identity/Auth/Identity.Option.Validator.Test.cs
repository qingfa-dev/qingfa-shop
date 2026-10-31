using Application.BuildingBlocks.Security.Identity.Auth.Options;

using FluentValidation.TestHelper;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

/// <summary>Unit tests for <see cref="IdentityOptionValidator"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Configuration")]
public sealed class IdentityOptionValidatorTest
{
    private readonly IdentityOptionValidator _sut = new();

    [Fact(DisplayName = "Identity options should accept the defaults")]
    public void Defaults_ShouldPass()
    {
        TestValidationResult<IdentityApiOption> result = _sut.TestValidate(new IdentityApiOption());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory(DisplayName = "Identity options should require a default role")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDefaultRole_ShouldFail(string defaultRole)
    {
        IdentityApiOption options = new() { DefaultRole = defaultRole };

        TestValidationResult<IdentityApiOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.DefaultRole)
            .WithErrorCode(IdentityOptionOutcome.Failure.DefaultRoleRequired.Code);
    }

    [Theory(DisplayName = "Identity options should require an absolute public base URL")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative")]
    [InlineData("not-a-url")]
    public void RelativePublicBaseUrl_ShouldFail(string publicBaseUrl)
    {
        IdentityApiOption options = new() { PublicBaseUrl = publicBaseUrl };

        TestValidationResult<IdentityApiOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.PublicBaseUrl)
            .WithErrorCode(IdentityOptionOutcome.Failure.PublicBaseUrlInvalid.Code);
    }

    [Theory(DisplayName = "Identity options should require a confirmation path")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankConfirmationPath_ShouldFail(string confirmationPath)
    {
        IdentityApiOption options = new() { ConfirmationPath = confirmationPath };

        TestValidationResult<IdentityApiOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.ConfirmationPath)
            .WithErrorCode(IdentityOptionOutcome.Failure.ConfirmationPathRequired.Code);
    }

    [Theory(DisplayName = "Identity options should require a password reset path")]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankResetPasswordPath_ShouldFail(string resetPasswordPath)
    {
        IdentityApiOption options = new() { ResetPasswordPath = resetPasswordPath };

        TestValidationResult<IdentityApiOption> result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.ResetPasswordPath)
            .WithErrorCode(IdentityOptionOutcome.Failure.ResetPasswordPathRequired.Code);
    }
}
