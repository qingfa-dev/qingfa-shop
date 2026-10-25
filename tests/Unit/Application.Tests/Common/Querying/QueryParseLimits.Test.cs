using Application.Common.Querying;

namespace Application.Tests.Common.Querying;

public sealed class QueryParseLimitsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var limits = QueryParseLimits.Default;

        limits.DefaultPageSize.Should().Be(QueryConstant.Limits.Page.DefaultSize);
        limits.MaxPageSize.Should().Be(QueryConstant.Limits.Page.MaxSize);
        limits.MaxFilterLength.Should().Be(QueryConstant.Limits.Filter.MaxLength);
        limits.MaxFilterDepth.Should().Be(QueryConstant.Limits.Filter.MaxDepth);
        limits.MaxConditions.Should().Be(QueryConstant.Limits.Filter.MaxConditions);
        limits.MaxSortFields.Should().Be(QueryConstant.Limits.Order.MaxFields);
        limits.MaxSelectFields.Should().Be(QueryConstant.Limits.Select.MaxColumns);
        limits.MaxGroupFields.Should().Be(QueryConstant.Limits.Group.MaxKeys);
        limits.MaxAggregates.Should().Be(QueryConstant.Limits.Aggregate.MaxCount);
        limits.MaxSearchLength.Should().Be(QueryConstant.Limits.Search.MaxLength);
        limits.MaxSearchTerms.Should().Be(QueryConstant.Limits.Search.MaxTerms);
        limits.MaxSearchFields.Should().Be(QueryConstant.Limits.Search.MaxFields);
        limits.MaxSearchLanguage.Should().Be(QueryConstant.Limits.Search.MaxLanguageLength);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void DefaultPageSize_CanBeCustomized(int size)
    {
        var limits = new QueryParseLimits { DefaultPageSize = size };

        limits.DefaultPageSize.Should().Be(size);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void MaxPageSize_CanBeCustomized(int size)
    {
        var limits = new QueryParseLimits { MaxPageSize = size };

        limits.MaxPageSize.Should().Be(size);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void MaxFilterLength_CanBeCustomized(int length)
    {
        var limits = new QueryParseLimits { MaxFilterLength = length };

        limits.MaxFilterLength.Should().Be(length);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void MaxFilterDepth_CanBeCustomized(int depth)
    {
        var limits = new QueryParseLimits { MaxFilterDepth = depth };

        limits.MaxFilterDepth.Should().Be(depth);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void MaxConditions_CanBeCustomized(int count)
    {
        var limits = new QueryParseLimits { MaxConditions = count };

        limits.MaxConditions.Should().Be(count);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void MaxSortFields_CanBeCustomized(int fields)
    {
        var limits = new QueryParseLimits { MaxSortFields = fields };

        limits.MaxSortFields.Should().Be(fields);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(128)]
    public void MaxSelectFields_CanBeCustomized(int fields)
    {
        var limits = new QueryParseLimits { MaxSelectFields = fields };

        limits.MaxSelectFields.Should().Be(fields);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void MaxGroupFields_CanBeCustomized(int fields)
    {
        var limits = new QueryParseLimits { MaxGroupFields = fields };

        limits.MaxGroupFields.Should().Be(fields);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public void MaxAggregates_CanBeCustomized(int count)
    {
        var limits = new QueryParseLimits { MaxAggregates = count };

        limits.MaxAggregates.Should().Be(count);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void MaxSearchLength_CanBeCustomized(int length)
    {
        var limits = new QueryParseLimits { MaxSearchLength = length };

        limits.MaxSearchLength.Should().Be(length);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void MaxSearchTerms_CanBeCustomized(int terms)
    {
        var limits = new QueryParseLimits { MaxSearchTerms = terms };

        limits.MaxSearchTerms.Should().Be(terms);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void MaxSearchFields_CanBeCustomized(int fields)
    {
        var limits = new QueryParseLimits { MaxSearchFields = fields };

        limits.MaxSearchFields.Should().Be(fields);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void MaxSearchLanguage_CanBeCustomized(int length)
    {
        var limits = new QueryParseLimits { MaxSearchLanguage = length };

        limits.MaxSearchLanguage.Should().Be(length);
    }

    [Fact]
    public void DefaultInstance_IsShared()
    {
        var limits1 = QueryParseLimits.Default;
        var limits2 = QueryParseLimits.Default;

        limits1.Should().BeSameAs(limits2);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var l1 = new QueryParseLimits
        {
            DefaultPageSize = 25,
            MaxPageSize = 150,
            MaxFilterLength = 1024,
            MaxFilterDepth = 16,
            MaxConditions = 50,
            MaxSortFields = 4,
            MaxSelectFields = 16,
            MaxGroupFields = 4,
            MaxAggregates = 8,
            MaxSearchLength = 128,
            MaxSearchTerms = 4,
            MaxSearchFields = 4,
            MaxSearchLanguage = 4
        };
        var l2 = new QueryParseLimits
        {
            DefaultPageSize = 25,
            MaxPageSize = 150,
            MaxFilterLength = 1024,
            MaxFilterDepth = 16,
            MaxConditions = 50,
            MaxSortFields = 4,
            MaxSelectFields = 16,
            MaxGroupFields = 4,
            MaxAggregates = 8,
            MaxSearchLength = 128,
            MaxSearchTerms = 4,
            MaxSearchFields = 4,
            MaxSearchLanguage = 4
        };

        l1.Should().Be(l2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var l1 = new QueryParseLimits { DefaultPageSize = 10 };
        var l2 = new QueryParseLimits { DefaultPageSize = 20 };

        l1.Should().NotBe(l2);
    }
}