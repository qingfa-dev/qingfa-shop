using Application.Common.Persistence;

namespace Infrastructure.Persistence.Todos;

/// <summary>Schema-qualified table names for the Todo module.</summary>
internal static class TodoTable
{
    /// <summary>The schema that owns the Todo tables.</summary>
    public const string Schema = PersistenceConstant.Schema.Todo;

    /// <summary>Fully qualified todo table.</summary>
    public const string Todos = Schema + ".todos";

    /// <summary>Fully qualified todo list table.</summary>
    public const string Lists = Schema + ".todo_lists";

    /// <summary>Fully qualified todo category table.</summary>
    public const string Categories = Schema + ".todo_categories";
}
