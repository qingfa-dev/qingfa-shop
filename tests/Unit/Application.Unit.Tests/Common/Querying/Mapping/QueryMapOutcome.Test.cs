using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Mapping;


public sealed class QueryMapOutcomeTest
{
    [Fact(DisplayName = "Name Too Long Should Create Error")]
    public void NameTooLong_ShouldCreateError()
    {
        Error error = QueryMapOutcome.Failure.NameTooLong(64);

        error.Code.Should().Be("Query.Map.NameTooLong");
        error.Message.Should().Contain("64");
    }
}