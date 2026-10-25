using Core.Common.Querying;

namespace Core.Tests.Common.Querying;

public sealed class QueryOutcomeTest
{
    [Fact]
    public void NotGroupable_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.NotGroupable("field");

        error.Code.Should().Be("Query.Field.NotGroupable");
        error.Message.Should().Contain("field");
    }

    [Fact]
    public void OperatorUnsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.OperatorUnsupported("customOp");

        error.Code.Should().Be("Query.Operator.Unsupported");
        error.Message.Should().Contain("customOp");
    }

    [Fact]
    public void PageSizeInvalid_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.PageSizeInvalid(200);

        error.Code.Should().Be("Query.PageSize.Invalid");
        error.Message.Should().Contain("200");
    }

    [Fact]
    public void SearchNotConfigured_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.SearchNotConfigured;

        error.Code.Should().Be("Query.Search.NotConfigured");
    }

    [Fact]
    public void SearchTooLong_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.SearchTooLong(256);

        error.Code.Should().Be("Query.Search.TooLong");
        error.Message.Should().Contain("256");
    }

    [Fact]
    public void MixedShape_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.MixedShape;

        error.Code.Should().Be("Query.Tabular.MixedShape");
    }

    [Fact]
    public void FieldNotGrouped_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.FieldNotGrouped("field");

        error.Code.Should().Be("Query.Tabular.FieldNotGrouped");
        error.Message.Should().Contain("field");
    }

    [Fact]
    public void AggregateSyntax_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.AggregateSyntax("bad expression");

        error.Code.Should().Be("Query.Aggregate.Syntax");
        error.Message.Should().Contain("bad expression");
    }

    [Fact]
    public void AggregateUnsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.AggregateUnsupported("field");

        error.Code.Should().Be("Query.Aggregate.Unsupported");
        error.Message.Should().Contain("field");
    }

    [Fact]
    public void Unsupported_ShouldCreateError()
    {
        Error error = QueryOutcome.Failure.Unsupported("feature");

        error.Code.Should().Be("Query.Unsupported");
        error.Message.Should().Contain("feature");
    }
}