using Application.Common.Querying;

using Application.Common.Querying.Searching;

namespace Application.Unit.Tests.Common.Querying.Searching;

/// <summary>Tests for the non-throwing search-request factory.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Querying")]
public sealed class SearchRequestFactoryTest
{
    [Theory(DisplayName = "TryCreate should build the matching request type")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void TryCreate_WithBasicMode_ShouldReturnBasicRequest(SearchMode mode)
    {
        // Act
        Result<SearchRequest> result = SearchRequest.TryCreate("hello", mode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<BasicSearchRequest>();
        result.Value.Mode.Should().Be(mode);
    }

    [Fact]
    public void TryCreate_WithFullTextMode_ShouldReturnFullTextRequest()
    {
        // Act
        Result<SearchRequest> result = SearchRequest.TryCreate("hello", SearchMode.FullText);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<FullTextSearchRequest>();
    }

    [Theory(DisplayName = "TryCreate should reject a missing query")]
    [InlineData("")]
    [InlineData(" ")]
    public void TryCreate_WhenQueryIsMissing_ShouldReturnSearchRequired(string query)
    {
        // Act
        Result<SearchRequest> result = SearchRequest.TryCreate(query, SearchMode.Contains);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(QueryErrorCodes.SearchRequired);
    }

    [Fact]
    public void TryCreate_WhenQueryIsTooLong_ShouldReturnSearchTooLong()
    {
        // Arrange
        string query = new('a', QueryConstant.Limits.Search.MaxLength + 1);

        // Act
        Result<SearchRequest> result = SearchRequest.TryCreate(query, SearchMode.Contains);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(QueryErrorCodes.SearchTooLong);
    }

    [Fact]
    public void TryCreate_WhenModeIsUndefined_ShouldReturnModeMismatch()
    {
        // Act
        Result<SearchRequest> result = SearchRequest.TryCreate("hello", (SearchMode)999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(QueryErrorCodes.SearchModeMismatch);
    }
}
