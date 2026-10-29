using Application.Common.Security.Authorization.Domain;

namespace Application.Common.Security.Authorization.Registry;

/// <summary>Registry of the application's permissions and their default role assignments.</summary>
/// <remarks>
/// Permissions are data, not code: role and user claims assign these identifiers at runtime.
/// The registry is the single place where the built-in catalog and role defaults are declared,
/// so seeders and endpoint gates reference the same value objects.
/// </remarks>
public static class PermissionContext
{
    /// <summary>Identity permissions.</summary>
    public static class Identity
    {
        /// <summary>User administration.</summary>
        public static class Users
        {
            /// <summary>Manage every user.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("identity", "users", "all", "manage");

            /// <summary>Read every user.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("identity", "users", "all", "read");
        }

        /// <summary>Role administration.</summary>
        public static class Roles
        {
            /// <summary>Manage every role.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("identity", "roles", "all", "manage");

            /// <summary>Read every role.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("identity", "roles", "all", "read");
        }

        /// <summary>Permission administration.</summary>
        public static class Permissions
        {
            /// <summary>Manage permission assignments.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("identity", "permissions", "all", "manage");

            /// <summary>Read permission assignments.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("identity", "permissions", "all", "read");
        }

        /// <summary>The signed-in user's own profile.</summary>
        public static class Profile
        {
            /// <summary>Read the caller's profile.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("identity", "profile", "self", "read");

            /// <summary>Update the caller's profile.</summary>
            public static readonly PermissionMetadata Update =
                PermissionMetadata.Create("identity", "profile", "self", "update");
        }
    }

    /// <summary>Todo feature permissions.</summary>
    public static class Todo
    {
        /// <summary>Todo items.</summary>
        public static class Items
        {
            /// <summary>Manage every todo item.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("todo", "todos", "item", "manage");

            /// <summary>Read todo items.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("todo", "todos", "item", "read");

            /// <summary>Create todo items.</summary>
            public static readonly PermissionMetadata Create =
                PermissionMetadata.Create("todo", "todos", "item", "create");

            /// <summary>Update todo items.</summary>
            public static readonly PermissionMetadata Update =
                PermissionMetadata.Create("todo", "todos", "item", "update");

            /// <summary>Delete todo items.</summary>
            public static readonly PermissionMetadata Delete =
                PermissionMetadata.Create("todo", "todos", "item", "delete");
        }

        /// <summary>Todo lists.</summary>
        public static class Lists
        {
            /// <summary>Manage every todo list.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("todo", "lists", "list", "manage");

            /// <summary>Read todo lists.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("todo", "lists", "list", "read");

            /// <summary>Create todo lists.</summary>
            public static readonly PermissionMetadata Create =
                PermissionMetadata.Create("todo", "lists", "list", "create");

            /// <summary>Update todo lists.</summary>
            public static readonly PermissionMetadata Update =
                PermissionMetadata.Create("todo", "lists", "list", "update");

            /// <summary>Delete todo lists.</summary>
            public static readonly PermissionMetadata Delete =
                PermissionMetadata.Create("todo", "lists", "list", "delete");
        }

        /// <summary>Todo categories.</summary>
        public static class Categories
        {
            /// <summary>Manage every todo category.</summary>
            public static readonly PermissionMetadata Manage =
                PermissionMetadata.Create("todo", "categories", "category", "manage");

            /// <summary>Read todo categories.</summary>
            public static readonly PermissionMetadata Read =
                PermissionMetadata.Create("todo", "categories", "category", "read");

            /// <summary>Create todo categories.</summary>
            public static readonly PermissionMetadata Create =
                PermissionMetadata.Create("todo", "categories", "category", "create");

            /// <summary>Update todo categories.</summary>
            public static readonly PermissionMetadata Update =
                PermissionMetadata.Create("todo", "categories", "category", "update");

            /// <summary>Delete todo categories.</summary>
            public static readonly PermissionMetadata Delete =
                PermissionMetadata.Create("todo", "categories", "category", "delete");
        }
    }

    /// <summary>Permissions granted to administrators.</summary>
    public static readonly IReadOnlyList<PermissionMetadata> Admin =
    [
        Identity.Users.Manage,
        Identity.Users.Read,
        Identity.Roles.Manage,
        Identity.Roles.Read,
        Identity.Permissions.Manage,
        Identity.Permissions.Read,
        Identity.Profile.Read,
        Identity.Profile.Update,
        Todo.Items.Manage,
        Todo.Items.Read,
        Todo.Items.Create,
        Todo.Items.Update,
        Todo.Items.Delete,
        Todo.Lists.Manage,
        Todo.Lists.Read,
        Todo.Lists.Create,
        Todo.Lists.Update,
        Todo.Lists.Delete,
        Todo.Categories.Manage,
        Todo.Categories.Read,
        Todo.Categories.Create,
        Todo.Categories.Update,
        Todo.Categories.Delete,
    ];

    /// <summary>Permissions granted to managers.</summary>
    public static readonly IReadOnlyList<PermissionMetadata> Manager =
    [
        Identity.Users.Read,
        Identity.Roles.Read,
        Identity.Permissions.Read,
        Identity.Profile.Read,
        Identity.Profile.Update,
        Todo.Items.Read,
        Todo.Items.Create,
        Todo.Items.Update,
        Todo.Items.Delete,
        Todo.Lists.Read,
        Todo.Lists.Create,
        Todo.Lists.Update,
        Todo.Lists.Delete,
        Todo.Categories.Read,
        Todo.Categories.Create,
        Todo.Categories.Update,
        Todo.Categories.Delete,
    ];

    /// <summary>Permissions granted to standard users.</summary>
    public static readonly IReadOnlyList<PermissionMetadata> User =
    [
        Identity.Profile.Read,
        Identity.Profile.Update,
        Todo.Items.Read,
        Todo.Items.Create,
        Todo.Items.Update,
        Todo.Items.Delete,
        Todo.Lists.Read,
        Todo.Lists.Create,
        Todo.Lists.Update,
        Todo.Lists.Delete,
        Todo.Categories.Read,
        Todo.Categories.Create,
        Todo.Categories.Update,
        Todo.Categories.Delete,
    ];

    /// <summary>Every known permission, de-duplicated by identifier.</summary>
    public static readonly IReadOnlyList<PermissionMetadata> All =
    [
        .. Admin
            .Concat(Manager)
            .Concat(User)
            .DistinctBy(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase)
    ];

    private static readonly Dictionary<string, PermissionMetadata> s_byIdentifier =
        All.ToDictionary(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, List<PermissionMetadata>> s_byCategory = All
        .GroupBy(permission => permission.Category, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.ToList(),
            StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, List<PermissionMetadata>> s_byDomain = All
        .GroupBy(permission => permission.Domain, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.ToList(),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Checks whether an identifier belongs to the built-in catalog.</summary>
    /// <param name="identifier">The candidate identifier.</param>
    /// <returns><c>true</c> when the identifier is registered.</returns>
    public static bool IsKnown(string identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && s_byIdentifier.ContainsKey(identifier);

    /// <summary>Finds a registered permission by identifier.</summary>
    /// <param name="identifier">The candidate identifier.</param>
    /// <returns>The permission metadata, or <c>null</c> when it is not registered.</returns>
    public static PermissionMetadata? ByIdentifier(string identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && s_byIdentifier.TryGetValue(identifier, out PermissionMetadata? permission)
            ? permission
            : null;

    /// <summary>Returns every registered permission in a category.</summary>
    /// <param name="category">The category name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    public static IReadOnlyList<PermissionMetadata> ByCategory(string category) =>
        s_byCategory.TryGetValue(category, out List<PermissionMetadata>? permissions)
            ? permissions
            : [];

    /// <summary>Returns every registered permission in a domain.</summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The matching permissions, or an empty list.</returns>
    public static IReadOnlyList<PermissionMetadata> ByDomain(string domain) =>
        s_byDomain.TryGetValue(domain, out List<PermissionMetadata>? permissions)
            ? permissions
            : [];
}
