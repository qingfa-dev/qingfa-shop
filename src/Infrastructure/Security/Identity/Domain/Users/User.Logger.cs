namespace Infrastructure.Security.Identity.Domain.Users;

public static partial class UserLoggers
{
    public static partial class Management
    {
        [LoggerMessage(
            EventId = 1042,
            Level = LogLevel.Debug,
            Message = "[User.Created]: {UserName} ({Email}) with ID {UserId} by {ActionBy}")]
        public static partial void Created(ILogger logger,
            string UserName,
            string Email,
            Guid UserId,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1045,
            Level = LogLevel.Debug,
            Message = "[User.Updated]: {UserName} ({Email}) with ID {UserId} by {ActionBy}")]
        public static partial void Updated(ILogger logger,
            string UserName,
            string Email,
            Guid UserId,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1046,
            Level = LogLevel.Debug,
            Message = "[User.Deleted]: {UserName} ({Email}) with ID {UserId} by {ActionBy}")]
        public static partial void Deleted(ILogger logger,
            string UserName,
            string Email,
            Guid UserId,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1047,
            Level = LogLevel.Debug,
            Message = "[User.StatusToggled]: ID {UserId} is now IsActive = {IsActive} by {ActionBy}")]
        public static partial void StatusToggled(ILogger logger,
            Guid UserId,
            bool IsActive,
            string? ActionBy = "System");
    }

    public static partial class Auth
    {
        [LoggerMessage(
            EventId = 1038,
            Level = LogLevel.Debug,
            Message = "[Auth.LoginSucceeded]: User {UserId} logged in from IP: {IpAddress} (by {ActionBy})")]
        public static partial void LoginSucceeded(ILogger logger,
            Guid UserId,
            string? IpAddress,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1000,
            Level = LogLevel.Debug,
            Message = "[Auth.LoggedOut]: User {UserId} logged out. Reason: {Reason} (by {ActionBy})")]
        public static partial void LoggedOut(ILogger logger,
            Guid UserId,
            string? Reason,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Debug,
            Message =
                "[Auth.AllDevicesLoggedOut]: User {UserId} logged out from all {DeviceCount} devices. Reason: {Reason} (by {ActionBy})")]
        public static partial void AllDevicesLoggedOut(ILogger logger,
            Guid UserId,
            int DeviceCount,
            string? Reason,
            string? ActionBy = "System");
    }

    public static partial class Passwords
    {
        [LoggerMessage(
            EventId = 1029,
            Level = LogLevel.Debug,
            Message = "[Account.PasswordChanged]: {UserId} ({Email}) at {Timestamp} by {ActionBy}")]
        public static partial void PasswordChanged(ILogger logger,
            Guid UserId,
            string Email,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1032,
            Level = LogLevel.Debug,
            Message = "[Account.PasswordResetRequested]: {UserId} ({Email}) at {Timestamp} by {ActionBy}")]
        public static partial void PasswordResetRequested(ILogger logger,
            Guid UserId,
            string Email,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1035,
            Level = LogLevel.Debug,
            Message = "[Account.PasswordReset]: {UserId} ({Email}) at {Timestamp} by {ActionBy}")]
        public static partial void PasswordReset(ILogger logger,
            Guid UserId,
            string Email,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1030,
            Level = LogLevel.Debug,
            Message = "[Account.ConfirmationSent]: Notification sent to {Email} (by {ActionBy})")]
        public static partial void ConfirmationSent(ILogger logger,
            string Email,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1031,
            Level = LogLevel.Error,
            Message =
                "[Account.ConfirmationSentFailed]: Failed to send notification to {Email}. Reasons: {Errors} (by {ActionBy})")]
        public static partial void ConfirmationSentFailed(ILogger logger,
            string Email,
            string? Errors,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1043,
            Level = LogLevel.Debug,
            Message = "[User.PasswordSetupSent]: Notification sent to {Email} (ID: {UserId}) by {ActionBy}")]
        public static partial void
            PasswordSetupSent(ILogger logger,
                string Email,
                Guid UserId,
                string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1044,
            Level = LogLevel.Error,
            Message = "[User.PasswordSetupFailed]: Failed to send setup to {Email}. Reasons: {Errors} by {ActionBy}")]
        public static partial void PasswordSetupFailed(ILogger logger,
            string Email,
            string Errors,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1033,
            Level = LogLevel.Warning,
            Message = "Failed to send password reset notification to {UserId}")]
        public static partial void PasswordResetNotificationFailed(ILogger logger, Guid UserId, Exception ex);
    }

    public static partial class Emails
    {
        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Debug,
            Message = "[Account.EmailChangeRequested]: User {UserId} from {OldEmail} to {NewEmail} by {ActionBy}")]
        public static partial void EmailChangeRequested(ILogger logger,
            Guid UserId,
            string OldEmail,
            string NewEmail,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1005,
            Level = LogLevel.Debug,
            Message = "[Account.EmailVerified]: {UserId} ({Email}) at {Timestamp} by {ActionBy}")]
        public static partial void EmailVerified(ILogger logger,
            Guid UserId,
            string Email,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1009,
            Level = LogLevel.Debug,
            Message = "[Account.EmailChangeConfirmed]: {UserId} ({Email}) at {Timestamp} by {ActionBy}")]
        public static partial void EmailChangeConfirmed(ILogger logger,
            Guid UserId,
            string Email,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1039,
            Level = LogLevel.Debug,
            Message = "[Account.EmailVerificationRequested]: Processing for user {UserId} ({Email}) by {ActionBy}")]
        public static partial void EmailVerificationRequested(ILogger logger,
            Guid UserId,
            string Email,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1060,
            Level = LogLevel.Debug,
            Message = "[Account.EmailVerifiedNotificationRequested]: Processing for user {UserId} ({Email}) by {ActionBy}")]
        public static partial void EmailVerifiedNotificationRequested(ILogger logger,
            Guid UserId,
            string Email,
            string? ActionBy = "System");
    }

    public static partial class Profiles
    {
        [LoggerMessage(
            EventId = 1061,
            Level = LogLevel.Debug,
            Message = "[Profile.CreationStarted]: Processing for user {UserId} by {ActionBy}")]
        public static partial void ProfileCreationStarted(ILogger logger,
            Guid UserId,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1062,
            Level = LogLevel.Debug,
            Message = "[Profile.Created]: Profile {ProfileId} created for user {UserId} by {ActionBy}")]
        public static partial void ProfileCreated(ILogger logger,
            Guid UserId,
            Guid ProfileId,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1063,
            Level = LogLevel.Error,
            Message = "[Profile.CreationFailed]: Failed for user {UserId}. Reasons: {Errors} by {ActionBy}")]
        public static partial void ProfileCreationFailed(ILogger logger,
            Guid UserId,
            string Errors,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1064,
            Level = LogLevel.Debug,
            Message = "[Profile.AlreadyExists]: Profile already exists for user {UserId} by {ActionBy}")]
        public static partial void ProfileAlreadyExists(ILogger logger,
            Guid UserId,
            string? ActionBy = "System");
    }

    public static partial class Phones
    {
        [LoggerMessage(
            EventId = 1017,
            Level = LogLevel.Debug,
            Message = "[Account.PhoneVerificationCodeSent]: {UserId} ({PhoneNumber}) at {Timestamp} by {ActionBy}")]
        public static partial void PhoneVerificationCodeSent(ILogger logger,
            Guid UserId,
            string PhoneNumber,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1018,
            Level = LogLevel.Debug,
            Message = "[Account.PhoneChangeRequested]: Processing for {UserId} to {NewPhoneNumber} by {ActionBy}")]
        public static partial void PhoneChangeRequested(ILogger logger,
            Guid UserId,
            string NewPhoneNumber,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1022,
            Level = LogLevel.Debug,
            Message =
                "[Account.PhoneChangeSuccessNotificationRequested]: Processing for {UserId} ({PhoneNumber}) by {ActionBy}")]
        public static partial void PhoneChangeSuccessNotificationRequested(ILogger logger,
            Guid UserId,
            string PhoneNumber,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1021,
            Level = LogLevel.Debug,
            Message =
                "[Account.PhoneChanged]: {UserId} ({OldPhoneNumber} -> {NewPhoneNumber}) at {Timestamp} by {ActionBy}")]
        public static partial void PhoneChanged(ILogger logger,
            Guid UserId,
            string OldPhoneNumber,
            string NewPhoneNumber,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1023,
            Level = LogLevel.Debug,
            Message = "[Account.PhoneConfirmed]: {UserId} ({PhoneNumber}) at {Timestamp} by {ActionBy}")]
        public static partial void PhoneConfirmed(ILogger logger,
            Guid UserId,
            string PhoneNumber,
            DateTimeOffset Timestamp,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1025,
            Level = LogLevel.Debug,
            Message = "[Account.PhoneVerificationRequested]: Processing for {UserId} ({PhoneNumber}) by {ActionBy}")]
        public static partial void PhoneVerificationRequested(ILogger logger,
            Guid UserId,
            string PhoneNumber,
            string? ActionBy = "System");
    }

    public static partial class ExternalLogin
    {
        [LoggerMessage(
            EventId = 1070,
            Level = LogLevel.Debug,
            Message =
                "[Auth.ExternalLoginSucceeded]: User {UserId} logged in via {Provider} from IP: {IpAddress} (by {ActionBy})")]
        public static partial void ExternalLoginSucceeded(ILogger logger,
            Guid UserId,
            string Provider,
            string? IpAddress,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1071,
            Level = LogLevel.Debug,
            Message =
                "[Auth.ExternalUserCreated]: User {UserId} created via {Provider} with email {Email} (by {ActionBy})")]
        public static partial void ExternalUserCreated(ILogger logger,
            Guid UserId,
            string Provider,
            string Email,
            string? ActionBy = "System");
    }

    public static partial class Permissions
    {
        [LoggerMessage(
            EventId = 1051,
            Level = LogLevel.Debug,
            Message = "[User.PermissionsAssigned]: {Permissions} added to user {UserName} ({UserId}) by {ActionBy}")]
        public static partial void PermissionsAssigned(
            ILogger logger,
            string UserName,
            Guid UserId,
            string Permissions,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1052,
            Level = LogLevel.Debug,
            Message = "[User.PermissionsRevoked]: {Permissions} removed from user {UserName} ({UserId}) by {ActionBy}")]
        public static partial void PermissionsRevoked(
            ILogger logger,
            string UserName,
            Guid UserId,
            string Permissions,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1053,
            Level = LogLevel.Debug,
            Message =
                "[User.PermissionsSynced]: {UserName} ({UserId}) by {ActionBy}. Added: {AddedCount}, Removed: {RemovedCount}")]
        public static partial void PermissionsSynced(
            ILogger logger,
            string UserName,
            Guid UserId,
            int AddedCount,
            int RemovedCount,
            string? ActionBy = "System");
    }

    public static partial class Roles
    {
        [LoggerMessage(
            EventId = 1048,
            Level = LogLevel.Debug,
            Message = "[User.RolesAssigned]: {Roles} added to user {UserName} ({UserId}) by {ActionBy}")]
        public static partial void RolesAssigned(
            ILogger logger,
            string UserName,
            Guid UserId,
            string Roles,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1049,
            Level = LogLevel.Debug,
            Message = "[User.RolesRevoked]: {Roles} removed from user {UserName} ({UserId}) by {ActionBy}")]
        public static partial void RolesRevoked(
            ILogger logger,
            string UserName,
            Guid UserId,
            string Roles,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1050,
            Level = LogLevel.Debug,
            Message =
                "[User.RolesSynced]: {UserName} ({UserId}) by {ActionBy}. Added: {AddedCount}, Removed: {RemovedCount}")]
        public static partial void RolesSynced(
            ILogger logger,
            string UserName,
            Guid UserId,
            int AddedCount,
            int RemovedCount,
            string? ActionBy = "System");
    }
}
