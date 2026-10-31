using Application.BuildingBlocks.Querying;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

public static class RoleOutcome
{
    public static class Success
    {
        public const string Created = "Role created successfully.";
        public const string Updated = "Role updated successfully.";
        public const string Deleted = "Role deleted successfully.";
    }

    public static class Failure
    {
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Role must be specified.");

        public static Error NotFound => Error.NotFound(
            code: Codes.NotFound,
            message: "The specified role was not found.");

        public static Error NameRequired => Error.Validation(
            code: Codes.NameRequired,
            message: "Role name is required.");

        public static Error NameTooLong => Error.Validation(
            code: Codes.NameTooLong,
            message: $"Role name cannot exceed {RoleConstant.Constraints.Name.MaxLength} characters.");

        public static Error DescriptionTooLong => Error.Validation(
            code: Codes.DescriptionTooLong,
            message: $"Role description cannot exceed {RoleConstant.Constraints.Description.MaxLength} characters.");

        public static Error AlreadyExists => Error.Conflict(
            code: Codes.AlreadyExists,
            message: "A role with this name already exists.");

        // Enforce: Admin role must not be modified or deleted
        public static Error SystemRoleProtected => Error.Forbidden(
            code: Codes.SystemRoleProtected,
            message: "System roles cannot be modified or deleted.");


        public static Error CannotDeleteRoleWithUsers => Error.Validation(
            code: Codes.CannotDeleteRoleWithUsers,
            message: "Cannot delete a role that has assigned users.");

        /// <summary>Authentication required for role operations.</summary>
        public static Error AuthRequired => Error.Unauthorized(
            code: Codes.AuthRequired,
            message: "Authentication required.");

        /// <summary>
        /// Error when current user lacks the permission to assign a permission to others.
        /// </summary>
        public static Error AssignDenied(string permission) => Error.Forbidden(
            code: Codes.AssignDenied,
            message: $"You do not have the required permission '{permission}' to assign it to others.");

        /// <summary>
        /// Error when current user lacks the permission to revoke a permission from others.
        /// </summary>
        public static Error RevokeDenied(string permission) => Error.Forbidden(
            code: Codes.RevokeDenied,
            message: $"You do not have the required permission '{permission}' to revoke it from others.");

        /// <summary>An Identity operation failed.</summary>
        public static Error OperationFailed(string description) => Error.BadRequest(
            code: Codes.OperationFailed,
            message: description);

        /// <summary>A search parameter is not supported for role search.</summary>
        public static Error UnsupportedSearch(string parameter) => Error.BadRequest(
            code: Codes.UnsupportedSearch,
            message: $"{parameter} is not supported for role search.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Role.Entity.Required";
        public const string NotFound = "Role.NotFound";
        public const string NameRequired = "Role.Name.Required";
        public const string NameTooLong = "Role.Name.TooLong";
        public const string DescriptionTooLong = "Role.Description.TooLong";
        public const string AlreadyExists = "Role.AlreadyExists";
        public const string SystemRoleProtected = "Role.System.Protected";
        public const string CannotDeleteRoleWithUsers = "Role.CannotDeleteRoleWithUsers";
        public const string AuthRequired = "Role.AuthRequired";
        public const string AssignDenied = "Role.Permissions.AssignDenied";
        public const string RevokeDenied = "Role.Permissions.RevokeDenied";
        public const string OperationFailed = "Role.Operation.Failed";
        public const string UnsupportedSearch = QueryOutcome.Codes.Unsupported;
    }
}
