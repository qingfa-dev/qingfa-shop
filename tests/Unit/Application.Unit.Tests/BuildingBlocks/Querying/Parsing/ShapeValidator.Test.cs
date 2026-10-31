using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Parsing;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Parsing;


public sealed class ShapeValidatorTest
{
    [Fact(DisplayName = "Validate Select And Aggregate Without Group Returns Mixed Shape Failure")]
    public void Validate_SelectAndAggregateWithoutGroup_ReturnsMixedShapeFailure()
    {
        var select = new List<SelectField> { new("name", "name") };
        var aggregate = new List<AggregateSpec> { new(AggregateFunction.Count, "name", "cnt") };

        Result result = ShapeValidator.Validate(select, null, aggregate);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.MixedShape");
    }

    [Fact(DisplayName = "Validate Select Only No Group Returns Success")]
    public void Validate_SelectOnly_NoGroup_ReturnsSuccess()
    {
        var select = new List<SelectField> { new("name", "name") };

        Result result = ShapeValidator.Validate(select, null, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Group Only No Select Returns Success")]
    public void Validate_GroupOnly_NoSelect_ReturnsSuccess()
    {
        var group = new List<GroupByField> { new("kind") };

        Result result = ShapeValidator.Validate(null, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Aggregate Only No Group Returns Success")]
    public void Validate_AggregateOnly_NoGroup_ReturnsSuccess()
    {
        var aggregate = new List<AggregateSpec> { new(AggregateFunction.Count, "name", "cnt") };

        Result result = ShapeValidator.Validate(null, null, aggregate);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Select And Group All Selected Fields Grouped Returns Success")]
    public void Validate_SelectAndGroup_AllSelectedFieldsGrouped_ReturnsSuccess()
    {
        var select = new List<SelectField> { new("kind", "kind") };
        var group = new List<GroupByField> { new("kind"), new("name") };

        Result result = ShapeValidator.Validate(select, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Select And Group Selected Field Not Grouped Returns Failure")]
    public void Validate_SelectAndGroup_SelectedFieldNotGrouped_ReturnsFailure()
    {
        var select = new List<SelectField> { new("name", "name") };
        var group = new List<GroupByField> { new("kind") };

        Result result = ShapeValidator.Validate(select, group, null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == "Query.Tabular.FieldNotGrouped" && e.Message.Contains("name"));
    }

    [Fact(DisplayName = "Validate Select Subset Of Group Returns Success")]
    public void Validate_SelectSubsetOfGroup_ReturnsSuccess()
    {
        var select = new List<SelectField> { new("kind", "kind") };
        var group = new List<GroupByField> { new("kind"), new("name") };

        Result result = ShapeValidator.Validate(select, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Case Insensitive Field Matching Works")]
    public void Validate_CaseInsensitiveFieldMatching_Works()
    {
        var select = new List<SelectField> { new("NAME", "name") };
        var group = new List<GroupByField> { new("name") };

        Result result = ShapeValidator.Validate(select, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Empty Select With Group Returns Success")]
    public void Validate_EmptySelectWithGroup_ReturnsSuccess()
    {
        var select = new List<SelectField>();
        var group = new List<GroupByField> { new("kind") };

        Result result = ShapeValidator.Validate(select, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Null Select With Group Returns Success")]
    public void Validate_NullSelectWithGroup_ReturnsSuccess()
    {
        var group = new List<GroupByField> { new("kind") };

        Result result = ShapeValidator.Validate(null, group, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Null Group With Select Returns Success")]
    public void Validate_NullGroupWithSelect_ReturnsSuccess()
    {
        var select = new List<SelectField> { new("name", "name") };

        Result result = ShapeValidator.Validate(select, null, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate All Null Returns Success")]
    public void Validate_AllNull_ReturnsSuccess()
    {
        Result result = ShapeValidator.Validate(null, null, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate Select Group And Aggregate Selected Field Not Grouped Returns Failure")]
    public void Validate_SelectGroupAndAggregate_SelectedFieldNotGrouped_ReturnsFailure()
    {
        var select = new List<SelectField> { new("name", "name") };
        var group = new List<GroupByField> { new("kind") };
        var aggregate = new List<AggregateSpec> { new(AggregateFunction.Count, "name", "cnt") };

        Result result = ShapeValidator.Validate(select, group, aggregate);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.FieldNotGrouped");
    }

    [Fact(DisplayName = "Validate Select Group And Aggregate All Selected Grouped Returns Success")]
    public void Validate_SelectGroupAndAggregate_AllSelectedGrouped_ReturnsSuccess()
    {
        var select = new List<SelectField> { new("kind", "kind") };
        var group = new List<GroupByField> { new("kind"), new("name") };
        var aggregate = new List<AggregateSpec> { new(AggregateFunction.Count, "name", "cnt") };

        Result result = ShapeValidator.Validate(select, group, aggregate);

        result.IsSuccess.Should().BeTrue();
    }
}