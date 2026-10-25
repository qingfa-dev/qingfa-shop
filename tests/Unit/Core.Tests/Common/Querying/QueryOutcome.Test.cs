using AwesomeAssertions;

using Core.Common.Monad.Errors;
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
}