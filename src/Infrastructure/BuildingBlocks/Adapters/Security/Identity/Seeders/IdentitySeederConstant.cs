namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Seeders;

/// <summary>Stable identities and ordering for the identity seeders.</summary>
public static class IdentitySeederConstant
{
    /// <summary>Seeder names used by registration and <c>DependsOn</c>.</summary>
    public static class Names
    {
        /// <summary>Role seeder name.</summary>
        public const string Roles = "seeder:identity-roles";

        /// <summary>User seeder name.</summary>
        public const string Users = "seeder:identity-users";
    }

    /// <summary>Execution order for the identity seeders.</summary>
    public static class Order
    {
        /// <summary>Role seeder order.</summary>
        public const int Roles = 30;

        /// <summary>User seeder order.</summary>
        public const int Users = 40;
    }
}
