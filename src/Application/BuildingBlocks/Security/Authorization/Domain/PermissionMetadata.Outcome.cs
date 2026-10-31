namespace Application.BuildingBlocks.Security.Authorization.Domain;

/// <summary>
/// Stable error results for permission metadata operations.
/// </summary>
public static class PermissionMetadataOutcome
{
    /// <summary>Builds stable codes for permission metadata failures.</summary>
    public static class Codes
    {
        /// <summary>Builds a stable error code for one metadata field.</summary>
        /// <param name="partName">The field name, for example <c>Domain</c>.</param>
        /// <param name="suffix">The failure suffix, for example <c>Required</c>.</param>
        /// <returns>The dotted error code.</returns>
        public static string Code(string partName, string suffix) =>
            $"Permission.{partName}.{suffix}";
    }

    /// <summary>Builds a stable error code for one metadata field.</summary>
    /// <param name="partName">The field name, for example <c>Domain</c>.</param>
    /// <param name="suffix">The failure suffix, for example <c>Required</c>.</param>
    /// <returns>The dotted error code.</returns>
    public static string Code(string partName, string suffix) =>
        Codes.Code(partName, suffix);

    /// <summary>Failures returned by permission metadata validation.</summary>
    public static class Failure
    {
        /// <summary>Creates the error for a required part.</summary>
        public static Error PartRequired(string partName) => Error.Validation(
            code: Code(partName, "Required"),
            message: $"{partName} is required and cannot be empty.");

        /// <summary>Creates the error for a part beyond the maximum length.</summary>
        public static Error PartTooLong(string partName, int length) => Error.Validation(
            code: Code(partName, "TooLong"),
            message: $"{partName} exceeds maximum length of {PermissionMetadataConstant.Constraints.MaxPartLength}. (Provided: {length})");

        /// <summary>Creates the error for a part with disallowed characters.</summary>
        public static Error InvalidPartChars(string partName, string? value) => Error.Validation(
            code: Code(partName, "InvalidChars"),
            message: $"{partName} contains invalid characters. Allowed: {PermissionMetadataConstant.Constraints.AllowedPartChars}. (Value: '{value}')");

        /// <summary>Creates the error for a display name beyond the maximum length.</summary>
        public static Error NameTooLong(int length) => Error.Validation(
            code: Code("Name", "TooLong"),
            message: $"Permission name exceeds maximum length of {PermissionMetadataConstant.Constraints.MaxNameLength}. (Provided: {length})");

        /// <summary>Creates the error for a description beyond the maximum length.</summary>
        public static Error DescriptionTooLong(int length) => Error.Validation(
            code: Code("Description", "TooLong"),
            message: $"Permission description exceeds maximum length of {PermissionMetadataConstant.Constraints.MaxDescriptionLength}. (Provided: {length})");

        /// <summary>Creates the error for a missing identifier.</summary>
        public static Error IdentifierRequired => Error.Validation(
            code: Code("Identifier", "Required"),
            message: "Permission identifier is required and cannot be empty.");

        /// <summary>Creates the error for a malformed identifier.</summary>
        public static Error InvalidIdentifierFormat(string? identifier) => Error.Validation(
            code: Code("Identifier", "InvalidFormat"),
            message: $"Invalid permission identifier format: '{identifier}'. Expected four dot-separated lowercase parts with allowed characters.");

        /// <summary>Creates the error for an identifier beyond the maximum length.</summary>
        public static Error IdentifierTooLong(int length) => Error.Validation(
            code: Code("Identifier", "TooLong"),
            message: $"Identifier exceeds maximum length of {PermissionMetadataConstant.Constraints.MaxIdentifierLength}. (Provided: {length})");
    }
}
