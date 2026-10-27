using System.Linq.Expressions;
using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Mapping;


public sealed class QueryFieldBuilderTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    [Fact(DisplayName = "Defaults All Capabilities Enabled Except Searchable")]
    public void Defaults_AllCapabilitiesEnabledExceptSearchable()
    {
        var builder = new QueryFieldBuilder();
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Filterable.Should().BeTrue();
        field.Sortable.Should().BeTrue();
        field.Selectable.Should().BeTrue();
        field.Groupable.Should().BeFalse(); // default is false
        field.Searchable.Should().BeFalse(); // default is false
        field.CaseInsensitive.Should().BeFalse(); // default is false
        field.AllowNullSearch.Should().BeTrue(); // default is true
    }

    [Fact(DisplayName = "Filterable Can Disable")]
    public void Filterable_CanDisable()
    {
        var builder = new QueryFieldBuilder().Filterable(false);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Filterable.Should().BeFalse();
    }

    [Fact(DisplayName = "Sortable Can Disable")]
    public void Sortable_CanDisable()
    {
        var builder = new QueryFieldBuilder().Sortable(false);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Sortable.Should().BeFalse();
    }

    [Fact(DisplayName = "Selectable Can Disable")]
    public void Selectable_CanDisable()
    {
        var builder = new QueryFieldBuilder().Selectable(false);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Selectable.Should().BeFalse();
    }

    [Fact(DisplayName = "Searchable Can Enable")]
    public void Searchable_CanEnable()
    {
        var builder = new QueryFieldBuilder().Searchable(true);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Searchable.Should().BeTrue();
    }

    [Fact(DisplayName = "Groupable Can Enable")]
    public void Groupable_CanEnable()
    {
        var builder = new QueryFieldBuilder().Groupable(true);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Groupable.Should().BeTrue();
    }

    [Fact(DisplayName = "Case Insensitive Can Enable")]
    public void CaseInsensitive_CanEnable()
    {
        var builder = new QueryFieldBuilder().CaseInsensitive(true);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.CaseInsensitive.Should().BeTrue();
    }

    [Fact(DisplayName = "Allow Null Search Can Disable")]
    public void AllowNullSearch_CanDisable()
    {
        var builder = new QueryFieldBuilder().AllowNullSearch(false);
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.AllowNullSearch.Should().BeFalse();
    }

    [Fact(DisplayName = "Convert With Stores Converter")]
    public void ConvertWith_StoresConverter()
    {
        var builder = new QueryFieldBuilder().ConvertWith(raw =>
            Result<QueryValue>.Success(new QueryValue(raw.ToUpperInvariant())));
        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Converter.Should().NotBeNull();
        field.Converter!("hello").Value.Value.Should().Be("HELLO");
    }

    [Fact(DisplayName = "Convert With Null Converter Throws")]
    public void ConvertWith_NullConverter_Throws()
    {
        var builder = new QueryFieldBuilder();

        Action act = () => builder.ConvertWith(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Chaining Returns Same Builder")]
    public void Chaining_ReturnsSameBuilder()
    {
        var builder = new QueryFieldBuilder();

        var result = builder.Filterable(false).Sortable(false).Selectable(false);

        result.Should().BeSameAs(builder);
    }

    [Fact(DisplayName = "Build Composite Creates Field With Composite Members")]
    public void BuildComposite_CreatesFieldWithCompositeMembers()
    {
        var builder = new QueryFieldBuilder();
        Expression<Func<Target, object?>>[] members = [x => x.Name, x => x.Score];
        QueryField field = builder.BuildComposite("any", members, typeof(object), null);

        field.Name.Should().Be("any");
        field.Composite.Should().HaveCount(2);
        field.IsCollection.Should().BeFalse();
        field.IsIndexed.Should().BeFalse();
    }

    [Fact(DisplayName = "Build Composite Uses Builder Settings")]
    public void BuildComposite_UsesBuilderSettings()
    {
        var builder = new QueryFieldBuilder().Sortable(true).Selectable(true).Groupable(true);
        Expression<Func<Target, object?>>[] members = [x => x.Name];
        QueryField field = builder.BuildComposite("any", members, typeof(string), null);

        field.Sortable.Should().BeTrue();
        field.Selectable.Should().BeTrue();
        field.Groupable.Should().BeTrue();
    }

    [Fact(DisplayName = "Build Composite When Builder Disables Returns Disabled")]
    public void BuildComposite_WhenBuilderDisables_ReturnsDisabled()
    {
        var builder = new QueryFieldBuilder().Sortable(false).Selectable(false).Groupable(false);
        Expression<Func<Target, object?>>[] members = [x => x.Name];
        QueryField field = builder.BuildComposite("any", members, typeof(string), null);

        field.Sortable.Should().BeFalse();
        field.Selectable.Should().BeFalse();
        field.Groupable.Should().BeFalse();
    }
}