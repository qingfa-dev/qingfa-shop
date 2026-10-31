using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Users")]
public sealed class UserTest
{
    private static User CreateUser() =>
        User.Create("alice", "alice@example.com", "Alice", "Smith").Value;

    [Fact(DisplayName = "Create should initialize a usable active account and raise UserCreated")]
    public void Create_ShouldInitializeAccount()
    {
        Result<User> result = User.Create("alice", "alice@example.com", "Alice", "Smith");

        result.IsSuccess.Should().BeTrue();
        User user = result.Value;
        user.UserName.Should().Be("alice");
        user.Email.Should().Be("alice@example.com");
        user.FirstName.Should().Be("Alice");
        user.LastName.Should().Be("Smith");
        user.FullName.Should().Be("Alice Smith");
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().NotBe(default);
        user.CreatedBy.Should().Be("System");
        user.Events.Should().ContainSingle().Which.Should().BeOfType<UserCreated>();
    }

    [Theory(DisplayName = "Create should reject a blank username or email")]
    [InlineData("", "alice@example.com", "User.Username.Required")]
    [InlineData("   ", "alice@example.com", "User.Username.Required")]
    [InlineData("alice", "", "User.Email.Required")]
    public void Create_ShouldRejectMissingCredentials(string userName, string email, string expectedCode)
    {
        Result<User> result = User.Create(userName, email, "Alice", "Smith");

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Theory(DisplayName = "Create should reject invalid profile fields")]
    [InlineData("bad name", "alice@example.com", "Alice", "Smith", "User.Username.Invalid")]
    [InlineData("alice", "not-an-email", "Alice", "Smith", "User.Email.Invalid")]
    [InlineData("alice", "alice@example.com", "", "Smith", "User.FirstName.Required")]
    public void Create_ShouldRejectInvalidProfileFields(
        string userName,
        string email,
        string firstName,
        string lastName,
        string expectedCode)
    {
        Result<User> result = User.Create(userName, email, firstName, lastName);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Create should reject overlong profile fields")]
    public void Create_ShouldRejectOverlongProfileFields()
    {
        User.Create(
            new string('a', UserConstant.Constraints.Username.MaxLength + 1),
            "alice@example.com",
            "Alice",
            "Smith").Errors[0].Code.Should().Be("User.Username.TooLong");

        User.Create(
            "alice",
            $"{new string('a', UserConstant.Constraints.Email.MaxLength)}@example.com",
            "Alice",
            "Smith").Errors[0].Code.Should().Be("User.Email.TooLong");

        User.Create(
            "alice",
            "alice@example.com",
            new string('a', UserConstant.Constraints.Name.MaxFirstNameLength + 1),
            "Smith").Errors[0].Code.Should().Be("User.FirstName.TooLong");

        User.Create(
            "alice",
            "alice@example.com",
            "Alice",
            new string('a', UserConstant.Constraints.Name.MaxLastNameLength + 1))
            .Errors[0].Code.Should().Be("User.LastName.TooLong");
    }

    [Fact(DisplayName = "Create should reject an invalid phone number")]
    public void Create_ShouldRejectInvalidPhoneNumber()
    {
        User.Create("alice", "alice@example.com", "Alice", "Smith", phoneNumber: "123")
            .Errors[0].Code.Should().Be("User.Phone.Invalid");
    }

    [Fact(DisplayName = "Update should change profile fields, stamp modification, and raise UserUpdated")]
    public void Update_ShouldChangeProfileAndStampModification()
    {
        User user = CreateUser();

        Result<User> result = Result<User>.Success(user).Update(
            email: "new@example.com",
            firstName: "Alicia",
            lastName: "Jones");

        result.IsSuccess.Should().BeTrue();
        user.Email.Should().Be("new@example.com");
        user.FirstName.Should().Be("Alicia");
        user.LastName.Should().Be("Jones");
        user.ModifiedAtUtc.Should().NotBeNull();
        user.ModifiedBy.Should().Be("System");
        user.Events.OfType<UserUpdated>().Should().ContainSingle();
    }

    [Fact(DisplayName = "Profile setters should validate and trim values")]
    public void ProfileSetters_ShouldValidateAndTrim()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);

        result.SetUserName("  alice2  ").IsSuccess.Should().BeTrue();
        user.UserName.Should().Be("alice2");
        result.SetUserName("bad name").Errors[0].Code.Should().Be("User.Username.Invalid");

        result.SetEmail("  new@example.com  ").IsSuccess.Should().BeTrue();
        user.Email.Should().Be("new@example.com");
        result.SetEmail("not-an-email").Errors[0].Code.Should().Be("User.Email.Invalid");

        result.SetPhoneNumber("  +12025550101  ").IsSuccess.Should().BeTrue();
        user.PhoneNumber.Should().Be("+12025550101");
        result.SetPhoneNumber("123").Errors[0].Code.Should().Be("User.Phone.Invalid");
        result.SetPhoneNumber("   ").Errors[0].Code.Should().Be("User.Phone.Required");

        result.SetFirstName("  Alicia  ").IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("Alicia");
        result.SetFirstName("   ").Errors[0].Code.Should().Be("User.FirstName.Required");

        result.SetLastName("  Jones  ").IsSuccess.Should().BeTrue();
        user.LastName.Should().Be("Jones");
    }

    [Fact(DisplayName = "Confirmation setters should be idempotent")]
    public void ConfirmationSetters_ShouldBeIdempotent()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);

        result.ConfirmEmail().IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        result.ConfirmEmail().IsSuccess.Should().BeTrue();
        result.UnconfirmEmail().IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse();

        result.ConfirmPhoneNumber().IsSuccess.Should().BeTrue();
        user.PhoneNumberConfirmed.Should().BeTrue();
        result.ConfirmPhoneNumber().IsSuccess.Should().BeTrue();
        result.UnconfirmPhoneNumber().IsSuccess.Should().BeTrue();
        user.PhoneNumberConfirmed.Should().BeFalse();
    }

    [Fact(DisplayName = "Disable and Enable should toggle the active flag and raise events")]
    public void DisableEnable_ShouldToggleActiveFlag()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);

        result.Disable().IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        result.Disable().IsFailure.Should().BeTrue();

        result.Enable().IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        result.Enable().IsFailure.Should().BeTrue();
        user.Events.OfType<UserDeactivated>().Should().ContainSingle();
        user.Events.OfType<UserActivated>().Should().ContainSingle();
    }

    [Fact(DisplayName = "Lock and Unlock should control the lockout window and raise events")]
    public void LockUnlock_ShouldControlLockout()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);
        DateTimeOffset end = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        result.Lock(end);
        user.LockoutEnabled.Should().BeTrue();
        user.LockoutEnd.Should().Be(end);

        result.Unlock();
        user.LockoutEnabled.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();
        user.Events.OfType<UserLocked>().Should().ContainSingle();
        user.Events.OfType<UserUnlocked>().Should().ContainSingle();
    }

    [Fact(DisplayName = "RecordSignIn should rotate session data and raise UserSignedIn")]
    public void RecordSignIn_ShouldRotateSessionData()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);

        result.RecordSignIn("10.0.0.1");
        user.CurrentSignInIp.Should().Be("10.0.0.1");
        user.CurrentSignInAtUtc.Should().NotBeNull();
        user.LastSignInIp.Should().BeNull();
        user.SignInCount.Should().Be(1);

        result.RecordSignIn("10.0.0.2");
        user.CurrentSignInIp.Should().Be("10.0.0.2");
        user.LastSignInIp.Should().Be("10.0.0.1");
        user.LastSignInAtUtc.Should().NotBeNull();
        user.SignInCount.Should().Be(2);
        user.Events.OfType<UserSignedIn>().Should().HaveCount(2);
    }

    [Fact(DisplayName = "Failed attempts should increment and reset")]
    public void FailedAttempts_ShouldIncrementAndReset()
    {
        User user = CreateUser();
        Result<User> result = Result<User>.Success(user);

        result.RecordFailedAttempt();
        user.AccessFailedCount.Should().Be(1);

        result.ResetFailedAttempts();
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact(DisplayName = "FullName should handle a missing last name")]
    public void FullName_ShouldHandleMissingLastName()
    {
        User user = User.Create("alice", "alice@example.com", "Alice", null).Value;

        user.FullName.Should().Be("Alice");
    }
}
