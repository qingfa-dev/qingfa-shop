namespace Infrastructure.Security.Identity.Domain.Users;

/// <summary>Monadic profile and session mutations for identity users.</summary>
public static class UserExtension
{
    /// <summary>Updates profile fields, validating each supplied value.</summary>
    /// <param name="result">The result carrying the user.</param>
    /// <param name="userName">The optional replacement username.</param>
    /// <param name="email">The optional replacement email.</param>
    /// <param name="firstName">The optional replacement first name.</param>
    /// <param name="lastName">The optional replacement last name.</param>
    /// <param name="phoneNumber">The optional replacement phone number.</param>
    /// <param name="emailConfirmed">The optional email-confirmation state.</param>
    /// <param name="phoneNumberConfirmed">The optional phone-confirmation state.</param>
    /// <returns>The updated result, or the first failure.</returns>
    public static Result<User> Update(
        this Result<User> result,
        string? userName = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        bool? emailConfirmed = null,
        bool? phoneNumberConfirmed = null)
    {
        return result
            .Ensure(_ => userName is null
                ? Result.Ok()
                : UserValidator.ValidateUsername(userName))
            .Ensure(_ => email is null
                ? Result.Ok()
                : UserValidator.ValidateEmail(email))
            .Ensure(_ => firstName is null
                ? Result.Ok()
                : UserValidator.ValidateFirstName(firstName))
            .Ensure(_ => lastName is null
                ? Result.Ok()
                : UserValidator.ValidateLastName(lastName))
            .Ensure(_ => phoneNumber is null
                ? Result.Ok()
                : ValidatePhone(phoneNumber))
            .Tap(user => ApplyUpdate(
                user,
                userName,
                email,
                firstName,
                lastName,
                phoneNumber,
                emailConfirmed,
                phoneNumberConfirmed));
    }

    /// <summary>Sets a validated username.</summary>
    public static Result<User> SetUserName(this Result<User> result, string userName)
    {
        string normalized = userName.Trim();

        return result
            .Ensure(_ => UserValidator.ValidateUsername(normalized))
            .Tap(user => user.UserName = normalized);
    }

    /// <summary>Sets a validated email address.</summary>
    public static Result<User> SetEmail(this Result<User> result, string email)
    {
        string normalized = email.Trim();

        return result
            .Ensure(_ => UserValidator.ValidateEmail(normalized))
            .Tap(user => user.Email = normalized);
    }

    /// <summary>Sets a validated phone number.</summary>
    public static Result<User> SetPhoneNumber(this Result<User> result, string? phoneNumber)
    {
        string normalized = phoneNumber?.Trim() ?? string.Empty;

        return result
            .Ensure(_ => ValidatePhone(normalized))
            .Tap(user => user.PhoneNumber = normalized);
    }

    /// <summary>Sets a validated first name.</summary>
    public static Result<User> SetFirstName(this Result<User> result, string firstName)
    {
        string normalized = firstName.Trim();

        return result
            .Ensure(_ => UserValidator.ValidateFirstName(normalized))
            .Tap(user => user.FirstName = normalized);
    }

    /// <summary>Sets an optional last name.</summary>
    public static Result<User> SetLastName(this Result<User> result, string lastName)
    {
        string normalized = lastName.Trim();

        return result
            .Ensure(_ => UserValidator.ValidateLastName(normalized))
            .Tap(user => user.LastName = normalized);
    }

    /// <summary>Marks the email as confirmed.</summary>
    public static Result<User> ConfirmEmail(this Result<User> result) =>
        result.Tap(user => user.EmailConfirmed = true);

    /// <summary>Marks the email as unconfirmed.</summary>
    public static Result<User> UnconfirmEmail(this Result<User> result) =>
        result.Tap(user => user.EmailConfirmed = false);

    /// <summary>Marks the phone number as confirmed.</summary>
    public static Result<User> ConfirmPhoneNumber(this Result<User> result) =>
        result.Tap(user => user.PhoneNumberConfirmed = true);

    /// <summary>Marks the phone number as unconfirmed.</summary>
    public static Result<User> UnconfirmPhoneNumber(this Result<User> result) =>
        result.Tap(user => user.PhoneNumberConfirmed = false);

    /// <summary>Activates the user.</summary>
    public static Result<User> Enable(this Result<User> result)
    {
        return result
            .Ensure(user => !user.IsActive, UserOutcome.Failure.AlreadyActive)
            .Tap(user =>
            {
                user.IsActive = true;
                Touch(user);
                user.RaiseEvent(new UserActivated(user.Id));
            });
    }

    /// <summary>Deactivates the user.</summary>
    public static Result<User> Disable(this Result<User> result)
    {
        return result
            .Ensure(user => user.IsActive, UserOutcome.Failure.AlreadyDeactivated)
            .Tap(user =>
            {
                user.IsActive = false;
                Touch(user);
                user.RaiseEvent(new UserDeactivated(user.Id));
            });
    }

    /// <summary>Locks the user until the supplied moment, or indefinitely.</summary>
    public static Result<User> Lock(
        this Result<User> result,
        DateTimeOffset? lockoutEnd = null)
    {
        return result.Tap(user =>
        {
            DateTimeOffset? end = lockoutEnd ?? DateTimeOffset.MaxValue;
            user.LockoutEnabled = true;
            user.LockoutEnd = end;
            Touch(user);
            user.RaiseEvent(new UserLocked(user.Id, end));
        });
    }

    /// <summary>Clears the user's lockout.</summary>
    public static Result<User> Unlock(this Result<User> result)
    {
        return result.Tap(user =>
        {
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            Touch(user);
            user.RaiseEvent(new UserUnlocked(user.Id));
        });
    }

    /// <summary>Records a successful sign-in and rotates session data.</summary>
    public static Result<User> RecordSignIn(this Result<User> result, string? ipAddress)
    {
        return result.Tap(user =>
        {
            user.LastSignInIp = user.CurrentSignInIp;
            user.LastSignInAtUtc = user.CurrentSignInAtUtc;
            user.CurrentSignInIp = ipAddress;
            user.CurrentSignInAtUtc = DateTimeOffset.UtcNow;
            user.LastLoginAtUtc = user.CurrentSignInAtUtc;
            user.SignInCount++;
            user.AccessFailedCount = 0;
            Touch(user);
            user.RaiseEvent(new UserSignedIn(user.Id, ipAddress));
        });
    }

    /// <summary>Records a failed sign-in attempt.</summary>
    public static Result<User> RecordFailedAttempt(this Result<User> result) =>
        result.Tap(user => user.AccessFailedCount++);

    /// <summary>Resets the failed sign-in counter.</summary>
    public static Result<User> ResetFailedAttempts(this Result<User> result) =>
        result.Tap(user => user.AccessFailedCount = 0);

    private static Result ValidatePhone(string phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber)
            ? Result.Fail(UserOutcome.Failure.PhoneRequired)
            : UserValidator.ValidatePhoneNumber(phoneNumber);

    private static void ApplyUpdate(
        User user,
        string? userName,
        string? email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        bool? emailConfirmed,
        bool? phoneNumberConfirmed)
    {
        bool changed = false;

        if (userName is not null && userName.Trim() != user.UserName)
        {
            user.UserName = userName.Trim();
            changed = true;
        }

        if (email is not null && email.Trim() != user.Email)
        {
            user.Email = email.Trim();
            changed = true;
        }

        if (firstName is not null && firstName.Trim() != user.FirstName)
        {
            user.FirstName = firstName.Trim();
            changed = true;
        }

        if (lastName is not null && lastName.Trim() != user.LastName)
        {
            user.LastName = lastName.Trim();
            changed = true;
        }

        if (phoneNumber is not null && phoneNumber.Trim() != user.PhoneNumber)
        {
            user.PhoneNumber = phoneNumber.Trim();
            changed = true;
        }

        if (emailConfirmed.HasValue && emailConfirmed.Value != user.EmailConfirmed)
        {
            user.EmailConfirmed = emailConfirmed.Value;
            changed = true;
        }

        if (phoneNumberConfirmed.HasValue &&
            phoneNumberConfirmed.Value != user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = phoneNumberConfirmed.Value;
            changed = true;
        }

        if (changed)
        {
            Touch(user);
            user.RaiseEvent(new UserUpdated(user.Id));
        }
    }

    private static void Touch(User user)
    {
        user.ModifiedAtUtc = DateTimeOffset.UtcNow;
        user.ModifiedBy = "System";
    }
}
