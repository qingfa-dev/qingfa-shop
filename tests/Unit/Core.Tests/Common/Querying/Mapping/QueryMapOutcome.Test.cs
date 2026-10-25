using AwesomeAssertions;

using Core.Common.Monad.Errors;
using Core.Common.Querying;

namespace Core.Tests.Common.Querying;

public sealed class QueryMapOutcomeTest
{
    [Fact]
    public void NameTooLong_ShouldCreateError()
    {
        Error error = QueryMapOutcome.Failure.NameTooLong(64);

        error.Code.Should().Be("Query.Map.NameTooLong");
        error.Message.Should().Contain("64");
    }
}