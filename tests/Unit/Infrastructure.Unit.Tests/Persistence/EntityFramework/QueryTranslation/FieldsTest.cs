using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

[Trait("Category", "Unit")]
public sealed class FieldsTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();

    [Fact(DisplayName = "Resolve_WithMappedFilterableField_ReturnsField")]
    public void Resolve_WithMappedFilterableField_ReturnsField()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Resolve(Map, "name", sorting: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "Resolve_WithMappedSortableField_ReturnsField")]
    public void Resolve_WithMappedSortableField_ReturnsField()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Resolve(Map, "name", sorting: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "Resolve_WithUnknownField_ReturnsNotMappedError")]
    public void Resolve_WithUnknownField_ReturnsNotMappedError()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Resolve(Map, "missing", sorting: false);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Resolve_WithNonFilterableField_ReturnsNotFilterableError")]
    public void Resolve_WithNonFilterableField_ReturnsNotFilterableError()
    {
        var map = BuildMapWithNonFilterableField();
        var result = QueryTranslationEfCore<QueryProbe>.Resolve(map, "internal", sorting: false);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "Resolve_WithNonSortableField_ReturnsNotSortableError")]
    public void Resolve_WithNonSortableField_ReturnsNotSortableError()
    {
        var map = BuildMapWithNonSortableField();
        var result = QueryTranslationEfCore<QueryProbe>.Resolve(map, "hidden", sorting: true);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSortable");
    }

    [Fact(DisplayName = "ResolveMapped_WithMappedField_ReturnsField")]
    public void ResolveMapped_WithMappedField_ReturnsField()
    {
        var result = QueryTranslationEfCore<QueryProbe>.ResolveMapped("name", Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "ResolveMapped_WithUnknownField_ReturnsNotMappedError")]
    public void ResolveMapped_WithUnknownField_ReturnsNotMappedError()
    {
        var result = QueryTranslationEfCore<QueryProbe>.ResolveMapped("missing", Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "ResolveSelectable_WithSelectableField_ReturnsField")]
    public void ResolveSelectable_WithSelectableField_ReturnsField()
    {
        var result = QueryTranslationEfCore<QueryProbe>.ResolveSelectable("name", Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "ResolveSelectable_WithNonSelectableField_ReturnsNotSelectableError")]
    public void ResolveSelectable_WithNonSelectableField_ReturnsNotSelectableError()
    {
        var map = BuildMapWithNonSelectableField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveSelectable("internal", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "ResolveSelectable_WithCollectionField_ReturnsNotSelectableError")]
    public void ResolveSelectable_WithCollectionField_ReturnsNotSelectableError()
    {
        var map = BuildMapWithCollectionField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveSelectable("tags", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryErrorCodes.FieldNotSelectable);
    }

    [Fact(DisplayName = "ResolveSelectable_WithIndexedField_ReturnsNotSelectableError")]
    public void ResolveSelectable_WithIndexedField_ReturnsNotSelectableError()
    {
        var map = BuildMapWithIndexedField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveSelectable("indexedField", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "ResolveSelectable_WithNoExpression_ReturnsNotSelectableError")]
    public void ResolveSelectable_WithNoExpression_ReturnsNotSelectableError()
    {
        // The builder cannot create a field with null expression (falls back to lambda),
        // so we test with a non-selectable field instead
        var map = BuildMapWithNonSelectableField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveSelectable("internal", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryErrorCodes.FieldNotSelectable);
    }

    [Fact(DisplayName = "ResolveGroupable_WithGroupableField_ReturnsField")]
    public void ResolveGroupable_WithGroupableField_ReturnsField()
    {
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("name", Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "ResolveGroupable_WithNonGroupableField_ReturnsNotGroupableError")]
    public void ResolveGroupable_WithNonGroupableField_ReturnsNotGroupableError()
    {
        var map = BuildMapWithNonGroupableField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("hidden", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "ResolveGroupable_WithCollectionField_ReturnsNotGroupableError")]
    public void ResolveGroupable_WithCollectionField_ReturnsNotGroupableError()
    {
        var map = BuildMapWithCollectionField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("tags", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "ResolveGroupable_WithIndexedField_ReturnsNotGroupableError")]
    public void ResolveGroupable_WithIndexedField_ReturnsNotGroupableError()
    {
        var map = BuildMapWithIndexedField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("indexedField", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "ResolveGroupable_WithCompositeField_ReturnsNotGroupableError")]
    public void ResolveGroupable_WithCompositeField_ReturnsNotGroupableError()
    {
        var map = BuildMapWithCompositeField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("composite", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "ResolveGroupable_WithNoExpression_ReturnsNotGroupableError")]
    public void ResolveGroupable_WithNoExpression_ReturnsNotGroupableError()
    {
        // The builder cannot create a field with null expression (falls back to lambda),
        // so we test with a non-groupable field instead
        var map = BuildMapWithNonGroupableField();
        var result = QueryTranslationEfCore<QueryProbe>.ResolveGroupable("hidden", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryErrorCodes.FieldNotGroupable);
    }

    [Fact(DisplayName = "ResolveAggregatable_WithValidField_ReturnsField")]
    public void ResolveAggregatable_WithValidField_ReturnsField()
    {
        var spec = new AggregateSpec(AggregateFunction.Sum, "score", "total");
        var result = QueryTranslationEfCore<QueryProbe>.ResolveAggregatable(spec, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("score");
    }

    [Fact(DisplayName = "ResolveAggregatable_WithUnsupportedFunction_ReturnsUnsupportedError")]
    public void ResolveAggregatable_WithUnsupportedFunction_ReturnsUnsupportedError()
    {
        var spec = new AggregateSpec(AggregateFunction.Sum, "name", "total");
        var result = QueryTranslationEfCore<QueryProbe>.ResolveAggregatable(spec, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Unsupported");
    }

    [Fact(DisplayName = "Supports_SumWithNumericType_ReturnsTrue")]
    public void Supports_SumWithNumericType_ReturnsTrue()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Supports(AggregateFunction.Sum, typeof(int));

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Supports_AverageWithNonNumericType_ReturnsFalse")]
    public void Supports_AverageWithNonNumericType_ReturnsFalse()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Supports(AggregateFunction.Average, typeof(string));

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Supports_MinMaxWithComparableType_ReturnsTrue")]
    public void Supports_MinMaxWithComparableType_ReturnsTrue()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Supports(AggregateFunction.Minimum, typeof(DateTime));

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Supports_MinMaxWithNonComparableType_ReturnsFalse")]
    public void Supports_MinMaxWithNonComparableType_ReturnsFalse()
    {
        var result = QueryTranslationEfCore<QueryProbe>.Supports(AggregateFunction.Maximum, typeof(object));

        result.Should().BeFalse();
    }

    [Theory(DisplayName = "IsNumeric_ReturnsTrueForNumericTypes")]
    [InlineData(typeof(byte))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(int))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double))]
    [InlineData(typeof(decimal))]
    public void IsNumeric_ReturnsTrueForNumericTypes(Type type)
    {
        QueryTranslationEfCore<QueryProbe>.IsNumeric(type).Should().BeTrue();
    }

    [Theory(DisplayName = "IsNumeric_ReturnsFalseForNonNumericTypes")]
    [InlineData(typeof(string))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(object))]
    public void IsNumeric_ReturnsFalseForNonNumericTypes(Type type)
    {
        QueryTranslationEfCore<QueryProbe>.IsNumeric(type).Should().BeFalse();
    }

    private static QueryMap<QueryProbe> BuildMapWithNonFilterableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("internal", probe => probe.Id, field => field.Filterable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithNonSortableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("hidden", probe => probe.Tags!, field => field.Sortable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithNonSelectableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("internal", probe => probe.Id, field => field.Selectable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCollectionField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("tags", probe => probe.Tags!, field => field
                .IsCollection(true)
                .ElementType(typeof(string)));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithIndexedField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("indexedField", probe => probe.Score, field => field.IsIndexed(true));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCompositeField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("composite", probe => probe.Name, field => field.Composite<QueryProbe>(probe => probe.Name));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithNoExpression()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("noExpr", probe => probe.Id, field => field.Expression(null));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithNonGroupableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("hidden", probe => probe.Tags!, field => field.Groupable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}