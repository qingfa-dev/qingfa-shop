namespace Application.Common.Security.Authentication.Tokens.Domain;

/// <summary>Result constants for refresh token operations.</summary>
public static class RefreshTokenOutcome
{
    /// <summary>Failure error constants for refresh tokens.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the refresh token is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "RefreshToken.Entity.Required",
            message: "Refresh token must be specified.");

        /// <summary>Error returned when the refresh token cannot be found.</summary>
        public static Error NotFound => Error.NotFound(
            code: "RefreshToken.NotFound",
            message: "Refresh token not found.");

        /// <summary>Error returned when the refresh token has expired.</summary>
        public static Error Expired => Error.Unauthorized(
            code: "RefreshToken.Expired",
            message: "Refresh token has expired.");

        /// <summary>Error returned when the refresh token has been revoked.</summary>
        public static Error Revoked => Error.Unauthorized(
            code: "RefreshToken.Revoked",
            message: "Refresh token has been revoked.");

        /// <summary>Error returned when reuse of a rotated token indicates theft.</summary>
        public static Error TheftDetected => Error.Unauthorized(
            code: "RefreshToken.TheftDetected",
            message: "Token reuse detected. All user tokens have been revoked.");

        /// <summary>Error returned when the token hash is missing.</summary>
        public static Error TokenHashRequired => Error.BadRequest(
            code: "RefreshToken.TokenHash.Required",
            message: "Refresh token hash must be specified.");

        /// <summary>Error returned when the token hash exceeds its maximum length.</summary>
        public static Error TokenHashTooLong => Error.BadRequest(
            code: "RefreshToken.TokenHash.TooLong",
            message:
                $"Refresh token hash cannot exceed {RefreshTokenConstant.Constraints.TokenHash.MaxLength} characters.");

        /// <summary>Error returned when the owning user identifier is missing.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: "RefreshToken.UserId.Required",
            message: "Refresh token user identifier must be specified.");

        /// <summary>Error returned when the owning user identifier exceeds its maximum length.</summary>
        public static Error UserIdTooLong => Error.BadRequest(
            code: "RefreshToken.UserId.TooLong",
            message:
                $"Refresh token user identifier cannot exceed {RefreshTokenConstant.Constraints.UserId.MaxLength} characters.");

        /// <summary>Error returned when the token family identifier is missing.</summary>
        public static Error TokenFamilyIdRequired => Error.BadRequest(
            code: "RefreshToken.TokenFamilyId.Required",
            message: "Refresh token family identifier must be specified.");

        /// <summary>Error returned when the creation date is missing.</summary>
        public static Error CreatedAtRequired => Error.BadRequest(
            code: "RefreshToken.CreatedAt.Required",
            message: "Refresh token creation date must be specified.");

        /// <summary>Error returned when the expiry date is missing.</summary>
        public static Error ExpiresAtRequired => Error.BadRequest(
            code: "RefreshToken.ExpiresAt.Required",
            message: "Refresh token expiry date must be specified.");

        /// <summary>Error returned when the expiry does not follow the creation date.</summary>
        public static Error ExpiresAtBeforeCreated => Error.BadRequest(
            code: "RefreshToken.ExpiresAt.BeforeCreated",
            message: "Refresh token expiry must be later than its creation date.");

        /// <summary>Error returned when the revocation date precedes the creation date.</summary>
        public static Error RevokedAtBeforeCreated => Error.BadRequest(
            code: "RefreshToken.RevokedAt.BeforeCreated",
            message: "Refresh token revocation date cannot precede its creation date.");

        /// <summary>Error returned when a revoked token has no revocation reason.</summary>
        public static Error RevocationReasonRequired => Error.BadRequest(
            code: "RefreshToken.RevocationReason.Required",
            message: "Refresh token revocation reason must be specified when the token is revoked.");

        /// <summary>Error returned when the revocation reason is not a defined value.</summary>
        public static Error RevocationReasonInvalid => Error.BadRequest(
            code: "RefreshToken.RevocationReason.Invalid",
            message: "Refresh token revocation reason is not a defined value.");

        /// <summary>Error returned when a reason is supplied without a revocation date.</summary>
        public static Error RevocationReasonWithoutRevocation => Error.BadRequest(
            code: "RefreshToken.RevocationReason.WithoutRevocation",
            message: "Refresh token revocation reason requires a revocation date.");

        /// <summary>Error returned when rotation has no replacement token.</summary>
        public static Error ReplacementRequired => Error.BadRequest(
            code: "RefreshToken.Replacement.Required",
            message: "Replacement refresh token must be specified.");

        /// <summary>Error returned when a token is used as its own replacement.</summary>
        public static Error ReplacementSameToken => Error.Conflict(
            code: "RefreshToken.Replacement.SameToken",
            message: "A refresh token cannot replace itself.");

        /// <summary>Error returned when the replacement belongs to another token family.</summary>
        public static Error ReplacementFamilyMismatch => Error.Conflict(
            code: "RefreshToken.Replacement.FamilyMismatch",
            message: "Replacement refresh token must belong to the same token family.");

        /// <summary>Error returned when the replacement belongs to another user.</summary>
        public static Error ReplacementUserMismatch => Error.Conflict(
            code: "RefreshToken.Replacement.UserMismatch",
            message: "Replacement refresh token must belong to the same user.");

        /// <summary>Error returned when the replacement is expired or revoked.</summary>
        public static Error ReplacementNotActive => Error.Conflict(
            code: "RefreshToken.Replacement.NotActive",
            message: "Replacement refresh token must be active.");

        /// <summary>Error returned when token generation fails.</summary>
        public static Error GenerationFailed => Error.Unexpected(
            code: "RefreshToken.GenerationFailed",
            message: "Failed to generate refresh token.");

        /// <summary>Error returned when token rotation fails.</summary>
        public static Error RotationFailed => Error.Unexpected(
            code: "RefreshToken.RotationFailed",
            message: "Failed to rotate refresh token.");

        /// <summary>Error returned when token revocation fails.</summary>
        public static Error RevocationFailed => Error.Unexpected(
            code: "RefreshToken.RevocationFailed",
            message: "Failed to revoke refresh token.");

        /// <summary>Error returned when reuse detection fails.</summary>
        public static Error TokenTheftDetectorFailure => Error.Unexpected(
            code: "RefreshToken.TokenTheftDetectorFailure",
            message: "Failed to detect token reuse due to infrastructure error.");

        /// <summary>Error returned when the token store cannot be accessed.</summary>
        public static Error StoreFailure => Error.Unexpected(
            code: "RefreshToken.StoreFailure",
            message: "Failed to access refresh token store.");
    }
}
