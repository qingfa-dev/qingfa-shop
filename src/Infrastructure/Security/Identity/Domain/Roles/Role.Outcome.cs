namespace Infrastructure.Security.Identity.Domain.Roles;

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
            code: "Role.Entity.Required",
            message: "Role must be specified.");

        public static Error NotFound => Error.NotFound(
            code: "Role.NotFound",
            message: "The specified role was not found.");

        public static Error NameRequired => Error.Validation(
            code: "Role.Name.Required",
            message: "Role name is required.");

        public static Error NameTooLong => Error.Validation(
            code: "Role.Name.TooLong",
            message: $"Role name cannot exceed {RoleConstant.Constraints.Name.MaxLength} characters.");

        public static Error DescriptionTooLong => Error.Validation(
            code: "Role.Description.TooLong",
            message: $"Role description cannot exceed {RoleConstant.Constraints.Description.MaxLength} characters.");

        public static Error AlreadyExists => Error.Conflict(
            code: "Role.AlreadyExists",
            message: "A role with this name already exists.");

        // Enforce: Admin role must not be modified or deleted
        public static Error SystemRoleProtected => Error.Forbidden(
            code: "Role.System.Protected",
            message: "System roles cannot be modified or deleted.");


        public static Error CannotDeleteRoleWithUsers => Error.Validation(
            code: "Role.CannotDeleteRoleWithUsers",
            message: "Cannot delete a role that has assigned users.");

        /// <summary>Authentication required for role operations.</summary>
        public static Error AuthRequired => Error.Unauthorized(
            code: "Role.AuthRequired",
            message: "Authentication required.");

        /// <summary>
        /// Error when current user lacks the permission to assign a permission to others.
        /// </summary>
        public static Error AssignDenied(string permission) => Error.Forbidden(
            code: "Role.Permissions.AssignDenied",
            message: $"You do not have the required permission '{permission}' to assign it to others.");

        /// <summary>
        /// Error when current user lacks the permission to revoke a permission from others.
        /// </summary>
        public static Error RevokeDenied(string permission) => Error.Forbidden(
            code: "Role.Permissions.RevokeDenied",
            message: $"You do not have the required permission '{permission}' to revoke it from others.");
    }
}
