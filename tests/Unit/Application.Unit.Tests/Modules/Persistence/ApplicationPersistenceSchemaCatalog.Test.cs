using Application.BuildingBlocks.Persistence;
using Application.Modules.Persistence;
using Application.Modules.Todos.Persistence;

namespace Application.Unit.Tests.Modules.Persistence;

[Trait("Category", "Unit")]
[Trait("Module", "Persistence")]
[Trait("Feature", "ApplicationPersistenceSchemaCatalog")]
public sealed class ApplicationPersistenceSchemaCatalogTests
{
    private static readonly PlatformPersistenceSchemaCatalog s_platform = new();
    private static readonly TodoPersistenceSchemaCatalog s_todos = new();

    private static ApplicationPersistenceSchemaCatalog CreateCatalog() =>
        new([s_platform, s_todos]);

    [Fact(DisplayName = "ApplicationPersistenceSchemaCatalog: should merge platform and module schemas")]
    public void Constructor_ShouldMergeSchemas()
    {
        ApplicationPersistenceSchemaCatalog catalog = CreateCatalog();

        catalog.Schemas.Should().Equal(
            PersistenceConstant.Schema.Identity,
            PersistenceConstant.Schema.Messaging,
            TodoPersistenceSchemaCatalog.SchemaName);
    }

    [Fact(DisplayName = "ApplicationPersistenceSchemaCatalog: should merge platform and module tables in order")]
    public void Constructor_ShouldMergeTablesInOrder()
    {
        ApplicationPersistenceSchemaCatalog catalog = CreateCatalog();
        string[] expected = [.. s_platform.Tables, "todos", "todo_lists", "todo_categories"];

        catalog.Tables.Should().Equal(expected);
    }

    [Fact(DisplayName = "ApplicationPersistenceSchemaCatalog: should de-duplicate schemas and tables")]
    public void Constructor_ShouldDeDuplicate()
    {
        var duplicate = new FakeSchemaCatalog(
            [PersistenceConstant.Schema.Identity, "extra"],
            ["users", "extra_table"]);

        var catalog = new ApplicationPersistenceSchemaCatalog([s_platform, s_todos, duplicate]);

        catalog.Schemas.Should().OnlyHaveUniqueItems();
        catalog.Tables.Should().OnlyHaveUniqueItems();
        catalog.Schemas.Should().Equal(
            PersistenceConstant.Schema.Identity,
            PersistenceConstant.Schema.Messaging,
            TodoPersistenceSchemaCatalog.SchemaName,
            "extra");
        catalog.Tables.Should().Contain("users").And.Contain("extra_table");
    }

    [Fact(DisplayName = "ApplicationPersistenceSchemaCatalog: null contributions should throw")]
    public void Constructor_WithNullCatalogs_ShouldThrow()
    {
        IEnumerable<IPersistenceSchemaCatalogContributor>? catalogs = null;

        var act = () => new ApplicationPersistenceSchemaCatalog(catalogs!);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("catalogs");
    }

    private sealed class FakeSchemaCatalog(
        IReadOnlyList<string> schemas,
        IReadOnlyList<string> tables) : IPersistenceSchemaCatalogContributor
    {
        public IReadOnlyList<string> Schemas { get; } = schemas;

        public IReadOnlyList<string> Tables { get; } = tables;
    }
}
