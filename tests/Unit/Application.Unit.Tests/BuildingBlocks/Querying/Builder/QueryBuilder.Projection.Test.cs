using System.Linq.Expressions;

using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Builder;

public sealed class QueryBuilderProjectionTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int Score { get; set; }
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable())
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("score", item => item.Score)
            .Build().Value;

    [Fact(DisplayName = "Select expression should set tabular shape")]
    public void Select_Expression_ShouldSetTabularShape()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select(item => item.Name)
            .BuildTabular();

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Select.Should().ContainSingle();
        result.Value.Source.Shape.Should().Be(QueryShape.Tabular);
    }

    [Fact(DisplayName = "GroupBy and aggregate should build a tabular query")]
    public void GroupBy_WithAggregate_ShouldBuild()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy(item => item.Active)
            .Count(item => item.Score)
            .BuildTabular();

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.GroupBy.Should().ContainSingle();
        result.Value.Source.Aggregate.Should().ContainSingle();
    }

    [Fact(DisplayName = "BuildTabular should reject an entity query")]
    public void BuildTabular_WithEntity_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).BuildTabular();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Builder.TabularShapeRequired");
    }

    [Fact(DisplayName = "BuildEntity should reject a tabular query")]
    public void BuildEntity_WithTabular_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select(item => item.Name)
            .BuildEntity();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Builder.EntityShapeRequired");
    }

    [Fact(DisplayName = "BuildProjected should return the projection")]
    public void BuildProjected_ShouldReturnProjection()
    {
        Expression<Func<Item, string>> projection = item => item.Name;

        var result = QueryBuilder<Item>.Create(CreateMap())
            .Project(projection)
            .BuildProjected<string>();

        result.IsSuccess.Should().BeTrue();
        result.Value.Projection.Should().BeSameAs(projection);
    }

    [Fact(DisplayName = "BuildProjected without Project should fail")]
    public void BuildProjected_WithoutProjection_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).BuildProjected<string>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Builder.ProjectionRequired");
    }

    [Fact(DisplayName = "BuildTabular should reject a projection as a shape conflict")]
    public void BuildTabular_WithProjection_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Project(item => item.Name)
            .Select(item => item.Name)
            .BuildTabular();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderShapeConflict);
    }

    [Fact(DisplayName = "BuildProjected should preserve an existing failed state before checking the projection")]
    public void BuildProjected_WhenStateFailed_ShouldPreserveFailure()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where("missing=1")
            .BuildProjected<string>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
        result.Errors.Should().NotContain(
            error => error.Code == QueryOutcome.Codes.BuilderProjectionRequired);
    }

    [Fact(DisplayName = "BuildTabular should reject select plus aggregate without group")]
    public void BuildTabular_WithSelectAndAggregateWithoutGroup_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select(item => item.Name)
            .Count(item => item.Score)
            .BuildTabular();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Tabular.MixedShape");
    }

    [Fact(DisplayName = "BuildTabular should reject a selected field outside the group")]
    public void BuildTabular_WithUngroupedSelect_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy(item => item.Active)
            .Select(item => item.Name)
            .BuildTabular();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Tabular.FieldNotGrouped");
    }

    [Fact(DisplayName = "Select should reject a duplicate expression field")]
    public void Select_WithDuplicateExpressionField_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select(item => item.Name, item => item.Name)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Select should reject a duplicate typed field")]
    public void Select_WithDuplicateTypedField_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select([new SelectField("name", "name"), new SelectField("name", "name")])
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Select should reject a duplicate typed alias")]
    public void Select_WithDuplicateTypedAlias_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select([new SelectField("name", "value"), new SelectField("score", "value")])
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Select should reject an empty typed alias")]
    public void Select_WithEmptyTypedAlias_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select([new SelectField("name", " ")])
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == "Query.Builder.ExpressionUnsupported");
    }

    [Fact(DisplayName = "AddSelect should reject a field already selected by an earlier call")]
    public void AddSelect_WithFieldFromEarlierCall_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select(item => item.Name)
            .AddSelect(item => item.Name)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldDuplicate);
    }

    [Fact(DisplayName = "AddSelect should reject an alias already used by an earlier call")]
    public void AddSelect_WithAliasFromEarlierCall_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select([new SelectField("name", "value")])
            .AddSelect([new SelectField("score", "value")])
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldDuplicate);
    }

    [Fact(DisplayName = "AddGroupBy should reject a key already grouped by an earlier call")]
    public void AddGroupBy_WithKeyFromEarlierCall_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy("active")
            .AddGroupBy("active")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldDuplicate);
    }

    [Fact(DisplayName = "Aggregate should reject an alias already used by an earlier call")]
    public void Aggregate_WithAliasFromEarlierCall_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Count(item => item.Score)
            .Count(item => item.Score)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldDuplicate);
    }

    [Fact(DisplayName = "Aggregate append should enforce the merged aggregate limit")]
    public void Aggregate_ExceedingMergedLimit_ShouldFail()
    {
        QueryBuilder<Item> builder = QueryBuilder<Item>.Create(CreateMap());

        for (int index = 0; index < 17; index++)
            builder = builder.Count(item => item.Score, $"total{index}");

        var result = builder.Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.AggregateSyntax);
    }
}