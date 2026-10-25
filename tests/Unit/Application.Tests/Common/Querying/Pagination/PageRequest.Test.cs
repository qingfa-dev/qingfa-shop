using Application.Common.Querying.Pagination;

namespace Application.Tests.Common.Querying.Pagination;


public sealed class PageRequestTest
{
    [Fact(DisplayName = "Constructor With Valid Values Sets Properties")]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var request = new PageRequest(3, 50);

        request.Page.Should().Be(3);
        request.PageSize.Should().Be(50);
    }

    [Theory(DisplayName = "Constructor Various Valid Combinations All Valid")]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(1, 200)]
    [InlineData(10, 50)]
    [InlineData(100, 10)]
    public void Constructor_VariousValidCombinations_AllValid(int page, int pageSize)
    {
        var request = new PageRequest(page, pageSize);

        request.Page.Should().Be(page);
        request.PageSize.Should().Be(pageSize);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var r1 = new PageRequest(5, 25);
        var r2 = new PageRequest(5, 25);

        r1.Should().Be(r2);
    }

    [Theory(DisplayName = "Record Equality Different Values Are Not Equal")]
    [InlineData(1, 20, 2, 20)]
    [InlineData(1, 20, 1, 50)]
    public void RecordEquality_DifferentValues_AreNotEqual(
        int page1, int size1, int page2, int size2)
    {
        var r1 = new PageRequest(page1, size1);
        var r2 = new PageRequest(page2, size2);

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new PageRequest(1, 20);
        var modified = original with { Page = 5 };

        modified.Page.Should().Be(5);
        modified.PageSize.Should().Be(20);
        original.Page.Should().Be(1);
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var request = new PageRequest(7, 30);
        var (page, pageSize) = request;

        page.Should().Be(7);
        pageSize.Should().Be(30);
    }

    [Fact(DisplayName = "To String Contains Values")]
    public void ToString_ContainsValues()
    {
        var request = new PageRequest(3, 50);
        string str = request.ToString();

        str.Should().Contain("3");
        str.Should().Contain("50");
    }
}