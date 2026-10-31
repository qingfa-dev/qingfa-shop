using Application.Modules.Todos.Persistence;

namespace Application.Unit.Tests.Modules.Todos.Persistence;

[Trait("Category", "Unit")]
[Trait("Module", "Todos")]
[Trait("Feature", "PersistenceSchemaCatalog")]
public sealed class TodoPersistenceSchemaCatalogTests
{
    [Fact(DisplayName = "TodoPersistenceSchemaCatalog: schema name should be todo")]
    public void SchemaName_ShouldBeTodo()
    {
        TodoPersistenceSchemaCatalog.SchemaName.Should().Be("todo");
    }

    [Fact(DisplayName = "TodoPersistenceSchemaCatalog: should own only the todo schema")]
    public void Schemas_ShouldContainOnlyTodo()
    {
        var catalog = new TodoPersistenceSchemaCatalog();

        catalog.Schemas.Should().Equal("todo");
    }

    [Fact(DisplayName = "TodoPersistenceSchemaCatalog: should own exactly the three Todo tables")]
    public void Tables_ShouldContainTheTodoTables()
    {
        var catalog = new TodoPersistenceSchemaCatalog();

        catalog.Tables.Should().Equal("todos", "todo_lists", "todo_categories");
    }
}
