using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

/// <summary>Edge cases for filter expression construction.</summary>
[Trait("Category", "Unit")]
public sealed class FilterBranchTest
{
    private static readonly QueryMap<BranchProbe> Map = BranchProbeMap.Create();
    private static readonly IQueryTranslator<BranchProbe> Translator =
        new QueryTranslationEfCore<BranchProbe>(QueryProbeOperators.Create());

    [Fact(DisplayName = "Unknown filter node should be rejected")]
    public void ApplyFilter_WithUnknownNode_ShouldFail()
    {
        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(),
            new UnknownFilterNode(),
            Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterSyntax);
    }

    [Fact(DisplayName = "Empty string on a nullable value field should build a null check")]
    public void ApplyFilter_WithEmptyStringOnNullableValue_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "nullableScore", FilterOperator.Equal, string.Empty, null, typeof(int?), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Empty string on a value field should build a default check")]
    public void ApplyFilter_WithEmptyStringOnValueField_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "number", FilterOperator.NotEqual, string.Empty, null, typeof(double), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Null on a non-nullable value field should fail")]
    public void ApplyFilter_WithNullOnValueField_ShouldFail()
    {
        var filter = new FilterCondition(
            "number", FilterOperator.Equal, "null", null, typeof(double), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterValueInvalid);
    }

    [Theory(DisplayName = "Null on a nullable value field should build a null check")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.NotEqual)]
    public void ApplyFilter_WithNullOnNullableValue_ShouldSucceed(FilterOperator op)
    {
        var filter = new FilterCondition(
            "nullableScore", op, "null", null, typeof(int?), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Ordering on an enum should widen to its underlying integer")]
    public void ApplyFilter_WithEnumOrdering_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "priority", FilterOperator.LessThan,
            "Medium", Priority.Medium, typeof(Priority), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Ordering on a nullable enum should widen to a nullable integer")]
    public void ApplyFilter_WithNullableEnumOrdering_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "nullablePriority", FilterOperator.GreaterThan,
            "Medium", Priority.Medium, typeof(Priority?), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Custom operator without a name should be rejected")]
    public void ApplyFilter_WithUnnamedCustomOperator_ShouldFail()
    {
        var filter = new FilterCondition(
            "name", FilterOperator.Custom, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterSyntax);
    }

    [Fact(DisplayName = "Unknown operator should be rejected")]
    public void ApplyFilter_WithUnknownOperator_ShouldFail()
    {
        var filter = new FilterCondition(
            "name", (FilterOperator)999, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterSyntax);
    }

    [Fact(DisplayName = "A collection element comparison that cannot be built should fail")]
    public void ApplyFilter_WhenCollectionElementComparisonFails_ShouldFail()
    {
        var filter = new FilterCondition(
            "tags", FilterOperator.LessThan, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterValueInvalid);
    }

    [Fact(DisplayName = "A reference collection element should be null-guarded")]
    public void ApplyFilter_WithReferenceCollectionElement_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "children", FilterOperator.Equal, "child",
            new BranchChild { Name = "child" }, typeof(BranchChild), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Primitive collections should not receive a null element guard")]
    [InlineData("values", 1, typeof(int))]
    [InlineData("scores", 1, typeof(int?))]
    public void ApplyFilter_WithPrimitiveCollectionElement_ShouldSucceed(
        string field,
        object value,
        Type valueType)
    {
        var filter = new FilterCondition(
            field, FilterOperator.Equal, value.ToString(), value, valueType, false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "A collection projection should guard the original collection source")]
    public void ApplyFilter_WithCollectionSelectExpression_ShouldSucceed()
    {
        Expression<Func<BranchProbe, object?>> expression =
            probe => probe.Children.Select(child => child.Name);

        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("childNames", probe => probe.Children, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(string))
                .Expression(expression));
        QueryMap<BranchProbe> map = builder.Build().Value;

        var filter = new FilterCondition(
            "childNames", FilterOperator.Contains, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "A field-level case-insensitive default should lower both sides")]
    public void ApplyFilter_WithFieldCaseInsensitiveDefault_ShouldMatchIgnoringCase()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("name", probe => probe.Name, field => field.Filterable().CaseInsensitive());
        QueryMap<BranchProbe> map = builder.Build().Value;
        var filter = new FilterCondition(
            "name", FilterOperator.Equal, "ALPHA", "ALPHA", typeof(string), false, null, null);
        var source = new List<BranchProbe> { new() { Id = Guid.NewGuid(), Name = "alpha" } }.AsQueryable();

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(source, filter, map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact(DisplayName = "A composite sub-member comparison failure should fail the condition")]
    public void ApplyFilter_WhenCompositeMemberComparisonFails_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .MapComposite(
                "composite",
                [probe => probe.Name, probe => probe.Child!.Name],
                field => field.Filterable());
        QueryMap<BranchProbe> map = builder.Build().Value;

        var filter = new FilterCondition(
            "composite", FilterOperator.LessThan, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterValueInvalid);
    }

    [Fact(DisplayName = "An indexed field without an index value should fail")]
    public void ApplyFilter_WithIndexedFieldAndNoIndex_ShouldFail()
    {
        var filter = new FilterCondition(
            "tag", FilterOperator.Equal, "x", "x", typeof(string), false, null, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "An indexed field with an index value should build the element access")]
    public void ApplyFilter_WithIndexedFieldAndIndex_ShouldSucceed()
    {
        var filter = new FilterCondition(
            "tag", FilterOperator.Equal, "x", "x", typeof(string), false, 0, null);

        Result<IQueryable<BranchProbe>> result = Translator.ApplyFilter(
            new List<BranchProbe>().AsQueryable(), filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>A filter node type the translator does not recognize.</summary>
    private sealed record UnknownFilterNode : FilterNode;
}
