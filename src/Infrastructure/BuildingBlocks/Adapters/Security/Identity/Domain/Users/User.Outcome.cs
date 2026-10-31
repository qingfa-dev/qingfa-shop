namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

/// <summary>
/// Contains success messages and error results for User operations.
/// </summary>
public static class UserOutcome
{
    /// <summary>
    /// Success messages for user operations.
    /// </summary>
    public static class Success
    {
        #region Messages

        /// <summary>
        /// [Success]: Registered
        /// </summary>
        public const string Registered = "User registered successfully.";

        /// <summary>
        /// [Success]: Logged in
        /// </summary>
        public const string LoggedIn = "User logged in successfully.";

        /// <summary>
        /// [Success]: Logged out
        /// </summary>
        public const string LoggedOut = "User logged out successfully.";

        /// <summary>
        /// [Success]: All devices logged out
        /// </summary>
        public const string AllDevicesLoggedOut = "User logged out from all devices successfully.";

        /// <summary>
        /// [Success]: Session retrieved
        /// </summary>
        public const string SessionRetrieved = "User session retrieved successfully.";

        /// <summary>
        /// [Success]: Token refreshed
        /// </summary>
        public const string TokenRefreshed = "Token refreshed successfully.";

        /// <summary>
        /// [Success]: Profile updated
        /// </summary>
        public const string ProfileUpdated = "Profile updated successfully.";

        /// <summary>
        /// [Success]: Password changed
        /// </summary>
        public const string PasswordChanged = "Password changed successfully.";

        /// <summary>
        /// [Success]: Password reset requested
        /// </summary>
        public const string PasswordResetRequested = "Password reset instructions sent if the email exists.";

        /// <summary>
        /// [Success]: Password reset
        /// </summary>
        public const string PasswordReset = "Password reset successfully.";

        /// <summary>
        /// [Success]: Deactivated
        /// </summary>
        public const string Deactivated = "User deactivated successfully.";

        /// <summary>
        /// [Success]: Activated
        /// </summary>
        public const string Activated = "User activated successfully.";

        /// <summary>
        /// [Success]: Role updated
        /// </summary>
        public const string RoleUpdated = "User role updated successfully.";

        /// <summary>
        /// [Success]: Get by id
        /// </summary>
        public const string GetById = "User details retrieved successfully.";

        /// <summary>
        /// [Success]: Get list
        /// </summary>
        public const string GetList = "Users retrieved successfully.";

        /// <summary>
        /// [Success]: Get profile
        /// </summary>
        public const string GetProfile = "Profile retrieved successfully.";

        /// <summary>
        /// [Success]: Deleted
        /// </summary>
        public const string Deleted = "User deleted successfully.";

        #endregion
    }

    /// <summary>
    /// Error results for user operations.
    /// </summary>
    public static class Failure
    {
        #region General

        /// <summary>
        /// [General]: User entity required
        /// </summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "User must be specified.");

        /// <summary>
        /// [General]: Id required
        /// </summary>
        public static Error IdRequired => Error.Validation(
            code: Codes.IdRequired,
            message: "User identifier is required.");

        /// <summary>
        /// [General]: Not found
        /// </summary>
        public static Error NotFound => Error.NotFound(
            code: Codes.NotFound,
            message: "The specified user was not found.");

        /// <summary>
        /// [General]: Credential required
        /// </summary>
        public static Error CredentialRequired => Error.Validation(
            code: Codes.CredentialRequired,
            message: "A credential (username, email, or phone number) is required.");

        /// <summary>
        /// [General]: Credential invalid
        /// </summary>
        public static Error CredentialInvalid => Error.Validation(
            code: Codes.CredentialInvalid,
            message: "The provided credential is not in a valid format.");

        /// <summary>
        /// [General]: Unauthorized
        /// </summary>
        public static Error Unauthorized => Error.Unauthorized(
            code: Codes.Unauthorized,
            message: "You are not authorized to perform this action.");

        #endregion

        #region Username

        /// <summary>
        /// [Username]: Required
        /// </summary>
        public static Error UsernameRequired => Error.Validation(
            code: Codes.UsernameRequired,
            message: "Username is required.");

        /// <summary>
        /// [Username]: Too short
        /// </summary>
        public static Error UsernameTooShort => Error.Validation(
            code: Codes.UsernameTooShort,
            message: $"Username must be at least {UserConstant.Constraints.Username.MinLength} characters.");

        /// <summary>
        /// [Username]: Too long
        /// </summary>
        public static Error UsernameTooLong => Error.Validation(
            code: Codes.UsernameTooLong,
            message: $"Username cannot exceed {UserConstant.Constraints.Username.MaxLength} characters.");

        /// <summary>
        /// [Username]: Invalid format
        /// </summary>
        public static Error UsernameInvalid => Error.Validation(
            code: Codes.UsernameInvalid,
            message: "Username can only contain letters, numbers, and underscores.");

        /// <summary>
        /// [Username]: Duplicate
        /// </summary>
        public static Error UsernameDuplicate => Error.Conflict(
            code: Codes.UsernameDuplicate,
            message: "A user with this username already exists.");

        #endregion

        #region Email

        /// <summary>
        /// [Email]: Required
        /// </summary>
        public static Error EmailRequired => Error.Validation(
            code: Codes.EmailRequired,
            message: "Email address is required.");

        /// <summary>
        /// [Email]: Too long
        /// </summary>
        public static Error EmailTooLong => Error.Validation(
            code: Codes.EmailTooLong,
            message: $"Email cannot exceed {UserConstant.Constraints.Email.MaxLength} characters.");

        /// <summary>
        /// [Email]: Invalid format
        /// </summary>
        public static Error EmailInvalid => Error.Validation(
            code: Codes.EmailInvalid,
            message: "Invalid email address format.");

        /// <summary>
        /// [Email]: Duplicate
        /// </summary>
        public static Error EmailDuplicate => Error.Conflict(
            code: Codes.EmailDuplicate,
            message: "A user with this email address already exists.");

        /// <summary>
        /// [Email]: Same as current
        /// </summary>
        public static Error EmailSameAsCurrent => Error.Conflict(
            code: Codes.EmailSameAsCurrent,
            message: "The new email is the same as your current email.");

        /// <summary>
        /// [Email]: Pending already
        /// </summary>
        public static Error EmailPendingAlready => Error.Conflict(
            code: Codes.EmailPendingAlready,
            message: "A confirmation email has already been sent to this address.");

        /// <summary>
        /// [Email]: No pending change
        /// </summary>
        public static Error EmailNoPendingChange => Error.Validation(
            code: Codes.EmailNoPendingChange,
            message: "There is no pending email change to confirm.");

        #endregion

        #region First Name

        /// <summary>
        /// [First Name]: Required
        /// </summary>
        public static Error FirstNameRequired => Error.Validation(
             code: Codes.FirstNameRequired,
             message: "First name is required.");

        /// <summary>
        /// [First Name]: Too long
        /// </summary>
        public static Error FirstNameTooLong => Error.Validation(
             code: Codes.FirstNameTooLong,
             message: $"First name cannot exceed {UserConstant.Constraints.Name.MaxFirstNameLength} characters.");

        #endregion

        #region Last Name

        /// <summary>
        /// [Last Name]: Required
        /// </summary>
        public static Error LastNameRequired => Error.Validation(
             code: Codes.LastNameRequired,
             message: "Last name is required.");

        /// <summary>
        /// [Last Name]: Too long
        /// </summary>
        public static Error LastNameTooLong => Error.Validation(
             code: Codes.LastNameTooLong,
             message: $"Last name cannot exceed {UserConstant.Constraints.Name.MaxLastNameLength} characters.");

        #endregion

        #region Password

        /// <summary>
        /// [Password]: Required
        /// </summary>
        public static Error PasswordRequired => Error.Validation(
             code: Codes.PasswordRequired,
             message: "Password is required.");

        /// <summary>
        /// [Password]: Too short
        /// </summary>
        public static Error PasswordTooShort => Error.Validation(
             code: Codes.PasswordTooShort,
             message: $"Password must be at least {UserConstant.Constraints.Password.MinLength} characters.");

        /// <summary>
        /// [Password]: Too long
        /// </summary>
        public static Error PasswordTooLong => Error.Validation(
             code: Codes.PasswordTooLong,
             message: $"Password cannot exceed {UserConstant.Constraints.Password.MaxLength} characters.");

        /// <summary>
        /// [Password]: Too weak
        /// </summary>
        public static Error PasswordTooWeak => Error.Validation(
             code: Codes.PasswordTooWeak,
             message: "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        /// <summary>
        /// [Password]: Mismatch
        /// </summary>
        public static Error PasswordMismatch => Error.Validation(
             code: Codes.PasswordMismatch,
             message: "Current password is incorrect.");

        /// <summary>
        /// [Credentials]: Invalid
        /// </summary>
        public static Error InvalidCredentials => Error.Unauthorized(
             code: Codes.InvalidCredentials,
             message: "Invalid email or password.");

        /// <summary>
        /// [Password]: No pending reset
        /// </summary>
        public static Error PasswordNoPendingReset => Error.Validation(
            code: Codes.PasswordNoPendingReset,
            message: "There is no pending password reset request.");

        /// <summary>
        /// [Current Password]: Required
        /// </summary>
        public static Error CurrentPasswordRequired => Error.Validation(
            code: Codes.CurrentPasswordRequired,
            message: "Current password is required.");

        #endregion
        #region Status

        /// <summary>
        /// [Status]: Inactive
        /// </summary>
        public static Error Inactive => Error.Forbidden(
             code: Codes.Inactive,
             message: "This user account has been deactivated.");

        /// <summary>
        /// [Status]: Already deactivated
        /// </summary>
        public static Error AlreadyDeactivated => Error.Conflict(
             code: Codes.AlreadyDeactivated,
             message: "This user account is already deactivated.");

        /// <summary>
        /// [Status]: Already active
        /// </summary>
        public static Error AlreadyActive => Error.Conflict(
             code: Codes.AlreadyActive,
             message: "This user account is already active.");

        #endregion

        #region Token

        /// <summary>
        /// [Token]: Invalid
        /// </summary>
        public static Error InvalidToken => Error.Validation(
            code: Codes.InvalidToken,
            message: "The provided token is invalid or has expired.");

        /// <summary>
        /// [Token]: Expired
        /// </summary>
        public static Error TokenExpired => Error.Validation(
            code: Codes.TokenExpired,
            message: "The provided token has expired.");

        /// <summary>
        /// [Token]: Required
        /// </summary>
        public static Error TokenRequired => Error.Validation(
            code: Codes.TokenRequired,
            message: "Token is required.");

        #endregion

        #region Phone

        /// <summary>
        /// [Phone]: Invalid format
        /// </summary>
        public static Error PhoneInvalid => Error.Validation(
            code: Codes.PhoneInvalid,
            message: "Invalid phone number format.");

        /// <summary>
        /// [Phone]: Too long
        /// </summary>
        public static Error PhoneTooLong => Error.Validation(
            code: Codes.PhoneTooLong,
            message: $"Phone number cannot exceed {UserConstant.Constraints.Phone.MaxLength} characters.");

        /// <summary>
        /// [Phone]: Required
        /// </summary>
        public static Error PhoneRequired => Error.Validation(
            code: Codes.PhoneRequired,
            message: "Phone number is required.");

        /// <summary>
        /// [Phone]: Same as current
        /// </summary>
        public static Error PhoneSameAsCurrent => Error.Conflict(
            code: Codes.PhoneSameAsCurrent,
            message: "The new phone is the same as your current phone.");

        /// <summary>
        /// [Phone]: Duplicate
        /// </summary>
        public static Error PhoneDuplicate => Error.Conflict(
            code: Codes.PhoneDuplicate,
            message: "A user with this phone number already exists.");

        /// <summary>
        /// [Phone]: Pending already
        /// </summary>
        public static Error PhonePendingAlready => Error.Conflict(
            code: Codes.PhonePendingAlready,
            message: "A confirmation has already been sent to this phone number.");

        /// <summary>
        /// [Phone]: No pending change
        /// </summary>
        public static Error PhoneNoPendingChange => Error.Validation(
            code: Codes.PhoneNoPendingChange,
            message: "There is no pending phone change to confirm.");

        /// <summary>
        /// [Phone]: OTP already sent
        /// </summary>
        public static Error PhoneOtpAlreadySent => Error.Validation(
            code: Codes.PhoneOtpAlreadySent,
            message: "A verification code has already been sent. Please try again later.");

        #endregion

        #region OTP

        /// <summary>
        /// [OTP]: Required
        /// </summary>
        public static Error OtpRequired => Error.Validation(
            code: Codes.OtpRequired,
            message: "OTP is required.");

        /// <summary>
        /// [OTP]: Too short
        /// </summary>
        public static Error OtpTooShort => Error.Validation(
            code: Codes.OtpTooShort,
            message: $"OTP must be at least {UserConstant.Constraints.Otp.MinLength} characters.");

        /// <summary>
        /// [OTP]: Too long
        /// </summary>
        public static Error OtpTooLong => Error.Validation(
            code: Codes.OtpTooLong,
            message: $"OTP cannot exceed {UserConstant.Constraints.Otp.MaxLength} characters.");

        /// <summary>
        /// [OTP]: Invalid format
        /// </summary>
        public static Error OtpInvalid => Error.Validation(
            code: Codes.OtpInvalid,
            message: "OTP must contain only digits.");

        #endregion

        #region Date of Birth

        /// <summary>
        /// [Date of Birth]: Future
        /// </summary>
        public static Error DateOfBirthFuture => Error.Validation(
            code: Codes.DateOfBirthFuture,
            message: "Date of birth cannot be in the future.");

        /// <summary>
        /// [Date of Birth]: Underage
        /// </summary>
        public static Error DateOfBirthUnderage => Error.Validation(
            code: Codes.DateOfBirthUnderage,
            message: $"User must be at least {UserConstant.Constraints.Age.Minimum} years old.");

        /// <summary>
        /// [Date of Birth]: Too old
        /// </summary>
        public static Error DateOfBirthTooOld => Error.Validation(
            code: Codes.DateOfBirthTooOld,
            message: $"Date of birth is invalid.");

        #endregion

        #region External Login

        /// <summary>
        /// [External Login]: Provider error
        /// </summary>
        public static Error ExternalLoginProviderError => Error.Unauthorized(
            code: Codes.ExternalLoginProviderError,
            message: "An error occurred during external authentication with the provider.");

        /// <summary>
        /// [External Login]: Email missing
        /// </summary>
        public static Error ExternalLoginEmailMissing => Error.Unauthorized(
            code: Codes.ExternalLoginEmailMissing,
            message: "The external authentication provider did not provide an email address.");

        /// <summary>
        /// [External Login]: Unsupported provider
        /// </summary>
        public static Error ExternalLoginUnsupportedProvider => Error.Validation(
            code: Codes.ExternalLoginUnsupportedProvider,
            message: "The specified external login provider is not supported.");

        /// <summary>
        /// [External Login]: Token invalid
        /// </summary>
        public static Error ExternalLoginTokenInvalid => Error.Unauthorized(
            code: Codes.ExternalLoginTokenInvalid,
            message: "The provided external authentication token is invalid or expired.");

        /// <summary>
        /// [External Login]: Profile creation failed
        /// </summary>
        public static Error ProfileCreationFailed => Error.Unexpected(
            code: Codes.ProfileCreationFailed,
            message: "User profile could not be created. Please contact support.");

        #endregion

        #region Permissions

        /// <summary>
        /// [Permissions]: Assign denied
        /// </summary>
        public static Error AssignDenied(string permission) => Error.Forbidden(
            code: Codes.AssignDenied,
            message: $"You do not have the required permission '{permission}' to assign it to others.");

        /// <summary>
        /// [Permissions]: Revoke denied
        /// </summary>
        public static Error RevokeDenied(string permission) => Error.Forbidden(
            code: Codes.RevokeDenied,
            message: $"You do not have the required permission '{permission}' to revoke it from others.");

        #endregion

        #region Admin

        /// <summary>
        /// [Admin]: Cannot toggle own account status
        /// </summary>
        public static Error SelfStatusToggle => Error.Forbidden(
            code: Codes.SelfStatusToggle,
            message: "Cannot toggle your own account status.");

        /// <summary>
        /// [Admin]: Cannot delete own account
        /// </summary>
        public static Error SelfDelete => Error.Forbidden(
            code: Codes.SelfDelete,
            message: "Cannot delete your own account.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "User.Entity.Required";
        public const string IdRequired = "User.IdRequired";
        public const string NotFound = "User.NotFound";
        public const string CredentialRequired = "User.Credential.Required";
        public const string CredentialInvalid = "User.Credential.Invalid";
        public const string Unauthorized = "User.Unauthorized";
        public const string UsernameRequired = "User.Username.Required";
        public const string UsernameTooShort = "User.Username.TooShort";
        public const string UsernameTooLong = "User.Username.TooLong";
        public const string UsernameInvalid = "User.Username.Invalid";
        public const string UsernameDuplicate = "User.Username.Duplicate";
        public const string EmailRequired = "User.Email.Required";
        public const string EmailTooLong = "User.Email.TooLong";
        public const string EmailInvalid = "User.Email.Invalid";
        public const string EmailDuplicate = "User.Email.Duplicate";
        public const string EmailSameAsCurrent = "User.Email.SameAsCurrent";
        public const string EmailPendingAlready = "User.Email.PendingAlready";
        public const string EmailNoPendingChange = "User.Email.NoPendingChange";
        public const string FirstNameRequired = "User.FirstName.Required";
        public const string FirstNameTooLong = "User.FirstName.TooLong";
        public const string LastNameRequired = "User.LastName.Required";
        public const string LastNameTooLong = "User.LastName.TooLong";
        public const string PasswordRequired = "User.Password.Required";
        public const string PasswordTooShort = "User.Password.TooShort";
        public const string PasswordTooLong = "User.Password.TooLong";
        public const string PasswordTooWeak = "User.Password.TooWeak";
        public const string PasswordMismatch = "User.Password.Mismatch";
        public const string InvalidCredentials = "User.Credentials.Invalid";
        public const string PasswordNoPendingReset = "User.Password.NoPendingReset";
        public const string CurrentPasswordRequired = "User.CurrentPassword.Required";
        public const string Inactive = "User.Inactive";
        public const string AlreadyDeactivated = "User.AlreadyDeactivated";
        public const string AlreadyActive = "User.AlreadyActive";
        public const string InvalidToken = "User.Token.Invalid";
        public const string TokenExpired = "User.Token.Expired";
        public const string TokenRequired = "User.Token.Required";
        public const string PhoneInvalid = "User.Phone.Invalid";
        public const string PhoneTooLong = "User.Phone.TooLong";
        public const string PhoneRequired = "User.Phone.Required";
        public const string PhoneSameAsCurrent = "User.Phone.SameAsCurrent";
        public const string PhoneDuplicate = "User.Phone.Duplicate";
        public const string PhonePendingAlready = "User.Phone.PendingAlready";
        public const string PhoneNoPendingChange = "User.Phone.NoPendingChange";
        public const string PhoneOtpAlreadySent = "User.Phone.OtpAlreadySent";
        public const string OtpRequired = "User.Otp.Required";
        public const string OtpTooShort = "User.Otp.TooShort";
        public const string OtpTooLong = "User.Otp.TooLong";
        public const string OtpInvalid = "User.Otp.Invalid";
        public const string DateOfBirthFuture = "User.DateOfBirth.Future";
        public const string DateOfBirthUnderage = "User.DateOfBirth.Underage";
        public const string DateOfBirthTooOld = "User.DateOfBirth.TooOld";
        public const string ExternalLoginProviderError = "User.ExternalLogin.ProviderError";
        public const string ExternalLoginEmailMissing = "User.ExternalLogin.EmailMissing";
        public const string ExternalLoginUnsupportedProvider = "User.ExternalLogin.UnsupportedProvider";
        public const string ExternalLoginTokenInvalid = "User.ExternalLogin.TokenInvalid";
        public const string ProfileCreationFailed = "User.ExternalLogin.ProfileCreationFailed";
        public const string AssignDenied = "User.Permissions.AssignDenied";
        public const string RevokeDenied = "User.Permissions.RevokeDenied";
        public const string SelfStatusToggle = "User.Status.Self";
        public const string SelfDelete = "User.Delete.Self";
    }
}
