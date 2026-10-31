using Application.BuildingBlocks.Security.Authorization;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

/// <summary>
/// Contains constant values for the Role domain.
/// </summary>
public static class RoleConstant
{
    /// <summary>
    /// Default role names and related collections.
    /// </summary>
    public static class Defaults
    {
        public const string Admin = AuthorizationConstant.Defaults.AdminRole;
        public const string Manager = "Manager";
        public const string User = "User";

        /// <summary>
        /// A list of all default role names.
        /// </summary>
        public static readonly string[] All = [Admin, Manager, User];
    }

    /// <summary>
    /// Validation constraints for role properties.
    /// </summary>
    public static class Constraints
    {
        public static class Name
        {
            public const int MaxLength = 64;
        }

        public static class Description
        {
            public const int MaxLength = 256;
        }
    }

    public static class Query
    {
        public static readonly string[] AllowedSearchFields =
        [
            nameof(Role.Name),
            nameof(Role.Description),
            nameof(Role.CreatedBy),
            nameof(Role.ModifiedBy)
        ];

        public static readonly string[] AllowedSortFields =
        [
            nameof(Role.Name),
            nameof(Role.IsSystem),
            nameof(Role.CreatedAtUtc),
            nameof(Role.ModifiedAtUtc)
        ];

        public static readonly string[] AllowedFilterFields =
        [
            nameof(Role.Name),
            nameof(Role.Description),
            nameof(Role.IsSystem),
            nameof(Role.CreatedAtUtc),
            nameof(Role.ModifiedAtUtc)
        ];
    }
}
