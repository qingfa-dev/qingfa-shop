using System.Linq.Expressions;
using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Mapping;


public sealed class QueryFieldBuilderTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
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

    [Fact(DisplayName = "Is Collection False Clears The Element Type")]
    public void IsCollection_False_ClearsTheElementType()
    {
        var builder = new QueryFieldBuilder().IsCollection(false);
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build(
            "name",
            expr,
            "Name",
            typeof(List<string>),
            typeof(string),
            indexType: null);

        field.ElementType.Should().BeNull();
        field.IsCollection.Should().BeFalse();
    }

    [Fact(DisplayName = "Is Indexed True Sets An Integer Index Type")]
    public void IsIndexed_True_SetsAnIntegerIndexType()
    {
        var builder = new QueryFieldBuilder().IsIndexed(true);
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, indexType: null);

        field.IsIndexed.Should().BeTrue();
        field.IndexType.Should().Be(typeof(int));
    }

    [Fact(DisplayName = "Member Path Overrides The Declared Path")]
    public void MemberPath_OverridesTheDeclaredPath()
    {
        var builder = new QueryFieldBuilder().MemberPath("Alias");
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.MemberPath.Should().Be("Alias");
    }

    [Fact(DisplayName = "Expression Overrides The Declared Expression")]
    public void Expression_OverridesTheDeclaredExpression()
    {
        Expression<Func<Target, object?>> replacement = x => x.Score;
        var builder = new QueryFieldBuilder().Expression(replacement);
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Expression.Should().BeSameAs(replacement);
    }

    [Fact(DisplayName = "Element Type Overrides The Detected Element Type")]
    public void ElementType_OverridesTheDetectedElementType()
    {
        var builder = new QueryFieldBuilder().ElementType(typeof(Guid));
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.ElementType.Should().Be(typeof(Guid));
        field.IsCollection.Should().BeTrue();
    }

    [Fact(DisplayName = "Composite Adds A Member To The Field")]
    public void Composite_AddsAMemberToTheField()
    {
        var builder = new QueryFieldBuilder().Composite<Target>(x => x.Alias);
        Expression<Func<Target, object?>> expr = x => x.Name;

        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Composite.Should().ContainSingle();
    }

    [Fact(DisplayName = "Capability Methods Without Arguments Re Enable The Capability")]
    public void CapabilityMethods_WithoutArguments_ReEnableTheCapability()
    {
        var builder = new QueryFieldBuilder()
            .Filterable(false)
            .Sortable(false)
            .Selectable(false)
            .Searchable(true)
            .Groupable(true);

        var reenabled = builder
            .Filterable()
            .Sortable()
            .Selectable()
            .Searchable(false)
            .Groupable(false);

        reenabled.Should().BeSameAs(builder);

        Expression<Func<Target, object?>> expr = x => x.Name;
        QueryField field = builder.Build("name", expr, "Name", typeof(string), null, null);

        field.Filterable.Should().BeTrue();
        field.Sortable.Should().BeTrue();
        field.Selectable.Should().BeTrue();
        field.Searchable.Should().BeFalse();
        field.Groupable.Should().BeFalse();
    }
}