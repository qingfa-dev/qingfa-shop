using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Users")]
public sealed class UserValidatorTest
{
    private static User ValidUser() =>
        User.Create("alice", "alice@example.com", "Alice", "Smith").Value;

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Validate should reject a missing user")]
    public void Validate_ShouldRejectMissingUser()
    {
        ShouldFailWith(UserValidator.Validate(null), "User.Entity.Required");
    }

    [Fact(DisplayName = "Validate should accept a valid user")]
    public void Validate_ShouldAcceptValidUser()
    {
        UserValidator.Validate(ValidUser()).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate should reject missing first and overlong last names")]
    public void Validate_ShouldRejectNames()
    {
        User user = ValidUser();
        user.FirstName = " ";
        ShouldFailWith(UserValidator.Validate(user), "User.FirstName.Required");

        user.FirstName = "Alice";
        user.LastName = new string('a', UserConstant.Constraints.Name.MaxLastNameLength + 1);
        ShouldFailWith(UserValidator.Validate(user), "User.LastName.TooLong");
    }

    [Theory(DisplayName = "ValidateUsername should enforce the canonical shape")]
    [InlineData(null, "User.Username.Required")]
    [InlineData("ab", "User.Username.TooShort")]
    [InlineData("bad name", "User.Username.Invalid")]
    public void ValidateUsername_ShouldRejectInvalid(string? name, string expectedCode)
    {
        ShouldFailWith(UserValidator.ValidateUsername(name), expectedCode);
    }

    [Fact(DisplayName = "ValidateUsername should reject an overlong name")]
    public void ValidateUsername_ShouldRejectLongName()
    {
        ShouldFailWith(
            UserValidator.ValidateUsername(
                new string('a', UserConstant.Constraints.Username.MaxLength + 1)),
            "User.Username.TooLong");
    }

    [Fact(DisplayName = "ValidateUsername should accept a valid name")]
    public void ValidateUsername_ShouldAcceptValidName()
    {
        UserValidator.ValidateUsername("alice").IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateEmail should enforce required, length, and format")]
    [InlineData(null, "User.Email.Required")]
    [InlineData("not-an-email", "User.Email.Invalid")]
    public void ValidateEmail_ShouldRejectInvalid(string? email, string expectedCode)
    {
        ShouldFailWith(UserValidator.ValidateEmail(email), expectedCode);
    }

    [Fact(DisplayName = "ValidateEmail should reject an overlong address")]
    public void ValidateEmail_ShouldRejectLongAddress()
    {
        string email = $"{new string('a', UserConstant.Constraints.Email.MaxLength)}@example.com";

        ShouldFailWith(UserValidator.ValidateEmail(email), "User.Email.TooLong");
    }

    [Fact(DisplayName = "ValidateEmail should accept a valid address")]
    public void ValidateEmail_ShouldAcceptValidAddress()
    {
        UserValidator.ValidateEmail("alice@example.com").IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidatePhoneNumber should accept null and valid values")]
    public void ValidatePhoneNumber_ShouldAcceptOptionalValue()
    {
        UserValidator.ValidatePhoneNumber(null).IsSuccess.Should().BeTrue();
        UserValidator.ValidatePhoneNumber("+12025550101").IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidatePhoneNumber should reject invalid values")]
    [InlineData("123", "User.Phone.Invalid")]
    public void ValidatePhoneNumber_ShouldRejectInvalid(string phone, string expectedCode)
    {
        ShouldFailWith(UserValidator.ValidatePhoneNumber(phone), expectedCode);
    }

    [Fact(DisplayName = "ValidatePhoneNumber should reject an overlong value")]
    public void ValidatePhoneNumber_ShouldRejectLongValue()
    {
        string phone = new('1', UserConstant.Constraints.Phone.MaxLength + 1);

        ShouldFailWith(UserValidator.ValidatePhoneNumber(phone), "User.Phone.TooLong");
    }

    [Theory(DisplayName = "ValidatePassword should enforce required, length, and strength")]
    [InlineData(null, false, "User.Password.Required")]
    [InlineData("short", false, "User.Password.TooShort")]
    [InlineData("lowercase1!ab", true, "User.Password.TooWeak")]
    public void ValidatePassword_ShouldRejectInvalid(
        string? password,
        bool strong,
        string expectedCode)
    {
        ShouldFailWith(UserValidator.ValidatePassword(password, strong), expectedCode);
    }

    [Fact(DisplayName = "ValidatePassword should accept a strong password")]
    public void ValidatePassword_ShouldAcceptStrongPassword()
    {
        UserValidator.ValidatePassword("StrongPass1!", true).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Password helpers should recognize valid values")]
    public void Helpers_ShouldRecognizeValidValues()
    {
        UserValidator.IsValidEmail("alice@example.com").Should().BeTrue();
        UserValidator.IsValidPhone("+12025550101").Should().BeTrue();
        UserValidator.IsValidUsername("alice").Should().BeTrue();
        UserValidator.BeStrongPassword("StrongPass1!").Should().BeTrue();
    }
}
