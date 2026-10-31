using System.Text.RegularExpressions;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

/// <summary>Validates user profile invariants without mutating the entity.</summary>
public static class UserValidator
{
    /// <summary>Validates the user's stable profile fields.</summary>
    /// <param name="user">The user to validate.</param>
    /// <returns>The unchanged user on success, or the first broken rule.</returns>
    public static Result<User> Validate(User? user)
    {
        if (user is null)
            return Result<User>.Fail(UserOutcome.Failure.EntityRequired);

        return Result<User>.Success(user)
            .Ensure(candidate => ValidateUsername(candidate.UserName))
            .Ensure(candidate => ValidateEmail(candidate.Email))
            .Ensure(candidate => ValidateFirstName(candidate.FirstName))
            .Ensure(candidate => ValidateLastName(candidate.LastName))
            .Ensure(candidate => ValidatePhoneNumber(candidate.PhoneNumber));
    }

    /// <summary>Validates a username.</summary>
    /// <param name="userName">The username to validate.</param>
    /// <returns>Success when the username is usable; otherwise a failure.</returns>
    public static Result ValidateUsername(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Fail(UserOutcome.Failure.UsernameRequired);

        if (userName.Length < UserConstant.Constraints.Username.MinLength)
            return Result.Fail(UserOutcome.Failure.UsernameTooShort);

        if (userName.Length > UserConstant.Constraints.Username.MaxLength)
            return Result.Fail(UserOutcome.Failure.UsernameTooLong);

        return IsValidUsername(userName)
            ? Result.Ok()
            : Result.Fail(UserOutcome.Failure.UsernameInvalid);
    }

    /// <summary>Validates a required first name.</summary>
    /// <param name="firstName">The first name to validate.</param>
    /// <returns>Success when the name is usable; otherwise a failure.</returns>
    public static Result ValidateFirstName(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Fail(UserOutcome.Failure.FirstNameRequired);

        return firstName.Length > UserConstant.Constraints.Name.MaxFirstNameLength
            ? Result.Fail(UserOutcome.Failure.FirstNameTooLong)
            : Result.Ok();
    }

    /// <summary>Validates an optional last name.</summary>
    /// <param name="lastName">The last name to validate.</param>
    /// <returns>Success when the name is usable; otherwise a failure.</returns>
    public static Result ValidateLastName(string? lastName)
    {
        return lastName is not null &&
            lastName.Length > UserConstant.Constraints.Name.MaxLastNameLength
            ? Result.Fail(UserOutcome.Failure.LastNameTooLong)
            : Result.Ok();
    }

    /// <summary>Validates an email address.</summary>
    /// <param name="email">The email to validate.</param>
    /// <returns>Success when the email is usable; otherwise a failure.</returns>
    public static Result ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail(UserOutcome.Failure.EmailRequired);

        if (email.Length > UserConstant.Constraints.Email.MaxLength)
            return Result.Fail(UserOutcome.Failure.EmailTooLong);

        return IsValidEmail(email)
            ? Result.Ok()
            : Result.Fail(UserOutcome.Failure.EmailInvalid);
    }

    /// <summary>Validates an optional phone number.</summary>
    /// <param name="phoneNumber">The phone number to validate.</param>
    /// <returns>Success when the phone number is usable; otherwise a failure.</returns>
    public static Result ValidatePhoneNumber(string? phoneNumber)
    {
        if (phoneNumber is null)
            return Result.Ok();

        if (phoneNumber.Length > UserConstant.Constraints.Phone.MaxLength)
            return Result.Fail(UserOutcome.Failure.PhoneTooLong);

        return IsValidPhone(phoneNumber)
            ? Result.Ok()
            : Result.Fail(UserOutcome.Failure.PhoneInvalid);
    }

    /// <summary>Validates a password against the configured policy.</summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="requireStrongPassword">Whether composition rules are required.</param>
    /// <returns>Success when the password is usable; otherwise a failure.</returns>
    public static Result ValidatePassword(string? password, bool requireStrongPassword = false)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Fail(UserOutcome.Failure.PasswordRequired);

        if (password.Length < UserConstant.Constraints.Password.MinLength)
            return Result.Fail(UserOutcome.Failure.PasswordTooShort);

        if (password.Length > UserConstant.Constraints.Password.MaxLength)
            return Result.Fail(UserOutcome.Failure.PasswordTooLong);

        return !requireStrongPassword || BeStrongPassword(password)
            ? Result.Ok()
            : Result.Fail(UserOutcome.Failure.PasswordTooWeak);
    }

    /// <summary>Checks whether text matches the email format.</summary>
    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, UserConstant.Patterns.Email.Regex);

    /// <summary>Checks whether text matches the phone format.</summary>
    public static bool IsValidPhone(string? value) =>
        !string.IsNullOrEmpty(value) && Regex.IsMatch(value, UserConstant.Patterns.Phone.Regex);

    /// <summary>Checks whether text matches the username format and length.</summary>
    public static bool IsValidUsername(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        if (value.Length < UserConstant.Constraints.Username.MinLength ||
            value.Length > UserConstant.Constraints.Username.MaxLength)
        {
            return false;
        }

        return Regex.IsMatch(value, UserConstant.Patterns.Username.Regex);
    }

    /// <summary>Checks whether a password satisfies every composition rule.</summary>
    public static bool BeStrongPassword(string? password) =>
        !string.IsNullOrEmpty(password) &&
        Regex.IsMatch(password, UserConstant.Patterns.Password.Regex) &&
        Regex.IsMatch(password, UserConstant.Patterns.Password.Lowercase) &&
        Regex.IsMatch(password, UserConstant.Patterns.Password.Digit) &&
        Regex.IsMatch(password, UserConstant.Patterns.Password.SpecialChar);
}
