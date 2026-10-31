using System.Linq.Expressions;
using System.Reflection;

using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Edge cases for expression construction and null guards.</summary>
[Trait("Category", "Unit")]
public sealed class ExpressionBranchTest
{
    [Fact(DisplayName = "BuildGuard should combine guards for every nullable navigation link")]
    public void BuildGuard_WithNestedNavigation_ShouldCombineGuards()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");
        Expression child = Expression.Property(parameter, nameof(BranchProbe.Child));
        Expression parent = Expression.Property(child, nameof(BranchChild.Parent));
        Expression name = Expression.Property(parent, nameof(BranchParent.Name));

        Expression? guard = QueryTranslationEfCore<BranchProbe>.BuildGuard(name, includeLeaf: true);

        guard.Should().NotBeNull();
        guard!.NodeType.Should().Be(ExpressionType.AndAlso);
    }

    [Fact(DisplayName = "BuildSafeObject should return a conditional when a navigation can be null")]
    public void BuildSafeObject_WithNullableNavigation_ShouldReturnConditional()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");
        Expression child = Expression.Property(parameter, nameof(BranchProbe.Child));
        Expression name = Expression.Property(child, nameof(BranchChild.Name));

        Expression result = QueryTranslationEfCore<BranchProbe>.BuildSafeObject(name);

        result.NodeType.Should().Be(ExpressionType.Conditional);
    }

    [Fact(DisplayName = "BuildGuard should inspect index expressions")]
    public void BuildGuard_WithIndexExpression_ShouldAddLeafGuard()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(IndexedProbe), "x");
        PropertyInfo indexer = typeof(IndexedProbe).GetProperty("Item")!;
        IndexExpression index = Expression.MakeIndex(
            parameter,
            indexer,
            [Expression.Constant(0)]);

        Expression? guard = QueryTranslationEfCore<IndexedProbe>.BuildGuard(index, includeLeaf: true);

        guard.Should().NotBeNull();
        guard!.NodeType.Should().Be(ExpressionType.NotEqual);
    }

    [Fact(DisplayName = "BuildMember should substitute an indexed field's index parameter")]
    public void BuildMember_WithIndexedFieldAndIndex_ShouldSucceed()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .MapIndexed<int>("tag", (probe, index) => probe.Tags[index]);
        QueryMap<BranchProbe> map = builder.Build().Value;
        QueryField field = map.Fields.Single();
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");

        Result<Expression> result = QueryTranslationEfCore<BranchProbe>.BuildMember(field, parameter, 0);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "BuildMember should reject an indexed field without an index value")]
    public void BuildMember_WithIndexedFieldAndNoIndex_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .MapIndexed<int>("tag", (probe, index) => probe.Tags[index]);
        QueryMap<BranchProbe> map = builder.Build().Value;
        QueryField field = map.Fields.Single();
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");

        Result<Expression> result = QueryTranslationEfCore<BranchProbe>.BuildMember(field, parameter, null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "BuildMember should reject a composite field with no scalar expression")]
    public void BuildMember_WithCompositeField_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .MapComposite("composite", [probe => probe.Name], field => field.Filterable());
        QueryMap<BranchProbe> map = builder.Build().Value;
        QueryField field = map.Fields.Single();
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");

        Result<Expression> result = QueryTranslationEfCore<BranchProbe>.BuildMember(field, parameter, null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "BuildMember should reject an index value on a non-indexed field")]
    public void BuildMember_WithIndexOnNonIndexedField_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("name", probe => probe.Name);
        QueryMap<BranchProbe> map = builder.Build().Value;
        QueryField field = map.Fields.Single();
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");

        Result<Expression> result = QueryTranslationEfCore<BranchProbe>.BuildMember(field, parameter, "x");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "BuildGuard should treat a nullable value leaf as nullable")]
    public void BuildGuard_WithNullableValueLeaf_ShouldAddGuard()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(BranchProbe), "x");
        Expression member = Expression.Property(parameter, nameof(BranchProbe.NullableScore));

        Expression? guard = QueryTranslationEfCore<BranchProbe>.BuildGuard(member, includeLeaf: true);

        guard.Should().NotBeNull();
    }

    /// <summary>Type with an indexer used to exercise index-expression guards.</summary>
    private sealed class IndexedProbe
    {
        public string this[int index] => index.ToString();
    }
}
