using Application.Common.Querying.Pagination;

namespace Application.Tests.Common.Querying.Pagination;

public sealed class QueryPageTest
{
    [Fact]
    public void Constructor_WithItemsPageSizeTotal_SetsProperties()
    {
        var items = new List<int> { 1, 2, 3 };
        var page = new QueryPage<int>(items, Page: 2, PageSize: 3, TotalCount: 8);

        page.Items.Should().Equal(items);
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(3);
        page.TotalCount.Should().Be(8);
    }

    [Fact]
    public void TotalPages_WithMultiplePages_ReturnsCorrectCount()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 8);

        page.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_WithExactDivision_ReturnsCorrectCount()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 3, PageSize: 3, TotalCount: 9);

        page.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_WithSinglePage_ReturnsOne()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 3);

        page.TotalPages.Should().Be(1);
    }

    [Fact]
    public void TotalPages_WithZeroTotalCount_ReturnsZero()
    {
        var page = new QueryPage<int>([], Page: 1, PageSize: 10, TotalCount: 0);

        page.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ReturnsZero()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 0, TotalCount: 3);

        page.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasPrevious_OnFirstPage_ReturnsFalse()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 3, TotalCount: 8);

        page.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_OnMiddlePage_ReturnsTrue()
    {
        var page = new QueryPage<int>([4, 5, 6], Page: 2, PageSize: 3, TotalCount: 8);

        page.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasPrevious_OnLastPage_ReturnsTrue()
    {
        var page = new QueryPage<int>([7, 8], Page: 3, PageSize: 3, TotalCount: 8);

        page.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasNext_OnLastPage_ReturnsFalse()
    {
        var page = new QueryPage<int>([], Page: 3, PageSize: 3, TotalCount: 8);

        page.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_OnMiddlePage_ReturnsTrue()
    {
        var page = new QueryPage<int>([4, 5, 6], Page: 2, PageSize: 3, TotalCount: 8);

        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_OnFirstPage_ReturnsTrue()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 3, TotalCount: 8);

        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_OnSinglePage_ReturnsFalse()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 3);

        page.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_OnSinglePage_ReturnsFalse()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 3);

        page.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var items = new[] { 1, 2 };
        var p1 = new QueryPage<int>(items, Page: 1, PageSize: 2, TotalCount: 5);
        var p2 = new QueryPage<int>(items, Page: 1, PageSize: 2, TotalCount: 5);

        p1.Should().Be(p2);
        p1.Equals(p2).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 2, 5, 2, 2, 5)]
    [InlineData(1, 2, 5, 1, 2, 6)]
    [InlineData(1, 2, 5, 1, 3, 5)]
    public void RecordEquality_DifferentValues_AreNotEqual(
        int page1, int size1, long total1, int page2, int size2, long total2)
    {
        var p1 = new QueryPage<int>([1], Page: page1, PageSize: size1, TotalCount: total1);
        var p2 = new QueryPage<int>([1], Page: page2, PageSize: size2, TotalCount: total2);

        p1.Should().NotBe(p2);
    }

    [Fact]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new QueryPage<int>([1, 2], Page: 1, PageSize: 2, TotalCount: 5);
        var modified = original with { Page = 2 };

        modified.Page.Should().Be(2);
        modified.PageSize.Should().Be(2);
        modified.TotalCount.Should().Be(5);
        original.Page.Should().Be(1);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        var page = new QueryPage<int>([1, 2], Page: 3, PageSize: 2, TotalCount: 5);
        var (items, pageNum, pageSize, total) = page;

        items.Should().Equal([1, 2]);
        pageNum.Should().Be(3);
        pageSize.Should().Be(2);
        total.Should().Be(5);
    }

    [Fact]
    public void PropertyGetters_AllAccessible()
    {
        var page = new QueryPage<int>([1, 2], Page: 3, PageSize: 2, TotalCount: 5);

        _ = page.Items;
        _ = page.Page;
        _ = page.PageSize;
        _ = page.TotalCount;
        _ = page.TotalPages;
        _ = page.HasPrevious;
        _ = page.HasNext;
    }
}