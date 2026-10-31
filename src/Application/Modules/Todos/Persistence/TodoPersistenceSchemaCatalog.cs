using Application.BuildingBlocks.Persistence;

namespace Application.Modules.Todos.Persistence;

/// <summary>Todos schema and tables.</summary>
public sealed class TodoPersistenceSchemaCatalog
    : IPersistenceSchemaCatalog, IPersistenceSchemaCatalogContributor
{
    /// <summary>The Todos schema name.</summary>
    public const string SchemaName = "todo";

    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } = [SchemaName];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
    [
        "todos",
        "todo_lists",
        "todo_categories"
    ];
}
