using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Mapping;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Branch tests for the field resolvers.</summary>
[Trait("Category", "Unit")]
public sealed class FieldsBranchTest
{
    private static readonly QueryMap<BranchProbe> Map = BranchProbeMap.Create();

    [Fact(DisplayName = "ResolveGroupable should reject an unmapped field")]
    public void ResolveGroupable_WithUnknownField_ShouldFail()
    {
        Result<QueryField> result = QueryTranslationEfCore<BranchProbe>.ResolveGroupable("missing", Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }

    [Fact(DisplayName = "ResolveAggregatable should reject an unmapped field")]
    public void ResolveAggregatable_WithUnknownField_ShouldFail()
    {
        var spec = new AggregateSpec(AggregateFunction.Count, "missing", "count");

        Result<QueryField> result = QueryTranslationEfCore<BranchProbe>.ResolveAggregatable(spec, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }

    [Fact(DisplayName = "ResolveAggregatable should reject a non-selectable field")]
    public void ResolveAggregatable_WithNonSelectableField_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("hidden", probe => probe.Name, field => field.Selectable(false));
        QueryMap<BranchProbe> map = builder.Build().Value;
        var spec = new AggregateSpec(AggregateFunction.Count, "hidden", "count");

        Result<QueryField> result = QueryTranslationEfCore<BranchProbe>.ResolveAggregatable(spec, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSelectable);
    }

    [Fact(DisplayName = "ResolveAggregatable should reject a collection field")]
    public void ResolveAggregatable_WithCollectionField_ShouldFail()
    {
        var spec = new AggregateSpec(AggregateFunction.Count, "tags", "count");

        Result<QueryField> result = QueryTranslationEfCore<BranchProbe>.ResolveAggregatable(spec, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSelectable);
    }

    [Fact(DisplayName = "ResolveAggregatable should reject an indexed field")]
    public void ResolveAggregatable_WithIndexedField_ShouldFail()
    {
        var spec = new AggregateSpec(AggregateFunction.Count, "tag", "count");

        Result<QueryField> result = QueryTranslationEfCore<BranchProbe>.ResolveAggregatable(spec, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSelectable);
    }
}
