using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Modules.Todos.Permissions;

/// <summary>Todos permission definitions and default role assignments.</summary>
public sealed class TodoPermissionCatalog : IPermissionCatalogContributor
{
    /// <summary>Todo item permissions.</summary>
    public static class Items
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "todos", "item", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "todos", "item", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "todos", "item", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "todos", "item", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "todos", "item", "delete");
    }

    /// <summary>Todo list permissions.</summary>
    public static class Lists
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "lists", "list", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "lists", "list", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "lists", "list", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "lists", "list", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "lists", "list", "delete");
    }

    /// <summary>Todo category permissions.</summary>
    public static class Categories
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "categories", "category", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "categories", "category", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "categories", "category", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "categories", "category", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "categories", "category", "delete");
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Permissions { get; } =
    [
        Items.Manage, Items.Read, Items.Create, Items.Update, Items.Delete,
        Lists.Manage, Lists.Read, Lists.Create, Lists.Update, Lists.Delete,
        Categories.Manage, Categories.Read, Categories.Create,
        Categories.Update, Categories.Delete
    ];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin => Permissions;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager => s_nonManage;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User => s_nonManage;

    // Preserve: managers and standard users never receive the aggregate manage grants.
    private static readonly IReadOnlyList<PermissionMetadata> s_nonManage =
    [
        Items.Read, Items.Create, Items.Update, Items.Delete,
        Lists.Read, Lists.Create, Lists.Update, Lists.Delete,
        Categories.Read, Categories.Create, Categories.Update, Categories.Delete
    ];
}
