using Application.Modules.Todos.Persistence;

namespace Infrastructure.Modules.Todos.Persistence.Schemas;

/// <summary>Schema-qualified table names for the Todo module.</summary>
internal static class TodoTable
{
    /// <summary>The schema that owns the Todo tables.</summary>
    public const string Schema = TodoPersistenceSchemaCatalog.SchemaName;

    /// <summary>Fully qualified todo table.</summary>
    public const string Todos = Schema + ".todos";

    /// <summary>Fully qualified todo list table.</summary>
    public const string Lists = Schema + ".todo_lists";

    /// <summary>Fully qualified todo category table.</summary>
    public const string Categories = Schema + ".todo_categories";
}
