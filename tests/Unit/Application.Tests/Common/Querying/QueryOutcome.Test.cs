using Application.Common.Querying;

namespace Core.Tests.Common.Querying;


public sealed class QueryOutcomeTest
{
    [Fact(DisplayName = "Not Groupable Should Create Error")]
    public void NotGroupable_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.NotGroupable("field");

        error.Code.Should().Be("Query.Field.NotGroupable");
        error.Message.Should().Contain("field");
    }

    [Fact(DisplayName = "Operator Unsupported Should Create Error")]
    public void OperatorUnsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.OperatorUnsupported("customOp");

        error.Code.Should().Be("Query.Operator.Unsupported");
        error.Message.Should().Contain("customOp");
    }

    [Fact(DisplayName = "Page Size Invalid Should Create Error")]
    public void PageSizeInvalid_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.PageSizeInvalid(200);

        error.Code.Should().Be("Query.PageSize.Invalid");
        error.Message.Should().Contain("200");
    }

    [Fact(DisplayName = "Search Not Configured Should Create Error")]
    public void SearchNotConfigured_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.SearchNotConfigured;

        error.Code.Should().Be("Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "Search Too Long Should Create Error")]
    public void SearchTooLong_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.SearchTooLong(256);

        error.Code.Should().Be("Query.Search.TooLong");
        error.Message.Should().Contain("256");
    }

    [Fact(DisplayName = "Mixed Shape Should Create Error")]
    public void MixedShape_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.MixedShape;

        error.Code.Should().Be("Query.Tabular.MixedShape");
    }

    [Fact(DisplayName = "Field Not Grouped Should Create Error")]
    public void FieldNotGrouped_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.FieldNotGrouped("field");

        error.Code.Should().Be("Query.Tabular.FieldNotGrouped");
        error.Message.Should().Contain("field");
    }

    [Fact(DisplayName = "Aggregate Syntax Should Create Error")]
    public void AggregateSyntax_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.AggregateSyntax("bad expression");

        error.Code.Should().Be("Query.Aggregate.Syntax");
        error.Message.Should().Contain("bad expression");
    }

    [Fact(DisplayName = "Aggregate Unsupported Should Create Error")]
    public void AggregateUnsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.AggregateUnsupported("field");

        error.Code.Should().Be("Query.Aggregate.Unsupported");
        error.Message.Should().Contain("field");
    }

    [Fact(DisplayName = "Unsupported Should Create Error")]
    public void Unsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.Unsupported("feature");

        error.Code.Should().Be("Query.Unsupported");
        error.Message.Should().Contain("feature");
    }
}