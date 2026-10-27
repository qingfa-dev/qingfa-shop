using Application.Common.Querying;

namespace Application.Tests.Common.Querying;


public sealed class QueryExecutionOptionsTest
{
    [Fact(DisplayName = "Default Should Have Expected Values")]
    public void Default_ShouldHaveExpectedValues()
    {
        var options = QueryExecutionOptions.Default;

        // Access all properties to ensure coverage
        options.IncludeSoftDeleted.Should().BeFalse();
        options.Distinct.Should().BeFalse();
        options.AsNoTracking.Should().BeTrue();
        options.AsSplitQuery.Should().BeFalse();
        options.Includes.Should().BeEmpty();
        options.MaxRows.Should().BeNull();
        options.CommandTimeout.Should().BeNull();
    }

    [Fact(DisplayName = "Default All Property Getters Are Accessible")]
    public void Default_AllPropertyGetters_AreAccessible()
    {
        // This test explicitly accesses all property getters on the default instance
        var options = QueryExecutionOptions.Default;

        _ = options.IncludeSoftDeleted;
        _ = options.Distinct;
        _ = options.AsNoTracking;
        _ = options.AsSplitQuery;
        _ = options.Includes;
        _ = options.MaxRows;
        _ = options.CommandTimeout;
    }

    [Theory(DisplayName = "Include Soft Deleted Can Be Set")]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeSoftDeleted_CanBeSet(bool value)
    {
        var options = new QueryExecutionOptions { IncludeSoftDeleted = value };

        options.IncludeSoftDeleted.Should().Be(value);
    }

    [Theory(DisplayName = "Distinct Can Be Set")]
    [InlineData(true)]
    [InlineData(false)]
    public void Distinct_CanBeSet(bool value)
    {
        var options = new QueryExecutionOptions { Distinct = value };

        options.Distinct.Should().Be(value);
    }

    [Theory(DisplayName = "As No Tracking Can Be Set")]
    [InlineData(true)]
    [InlineData(false)]
    public void AsNoTracking_CanBeSet(bool value)
    {
        var options = new QueryExecutionOptions { AsNoTracking = value };

        options.AsNoTracking.Should().Be(value);
    }

    [Theory(DisplayName = "As Split Query Can Be Set")]
    [InlineData(true)]
    [InlineData(false)]
    public void AsSplitQuery_CanBeSet(bool value)
    {
        var options = new QueryExecutionOptions { AsSplitQuery = value };

        options.AsSplitQuery.Should().Be(value);
    }

    [Fact(DisplayName = "Includes Can Be Set")]
    public void Includes_CanBeSet()
    {
        var includes = new[] { "Orders", "Orders.Items" };
        var options = new QueryExecutionOptions { Includes = includes };

        options.Includes.Should().Equal(includes);
    }

    [Fact(DisplayName = "Includes Default Is Empty Array")]
    public void Includes_DefaultIsEmptyArray()
    {
        var options = new QueryExecutionOptions();

        options.Includes.Should().BeEmpty();
    }

    [Theory(DisplayName = "Max Rows Can Be Set")]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MaxRows_CanBeSet(int maxRows)
    {
        var options = new QueryExecutionOptions { MaxRows = maxRows };

        options.MaxRows.Should().Be(maxRows);
    }

    [Fact(DisplayName = "Max Rows Default Is Null")]
    public void MaxRows_DefaultIsNull()
    {
        var options = new QueryExecutionOptions();

        options.MaxRows.Should().BeNull();
    }

    [Fact(DisplayName = "Command Timeout Can Be Set")]
    public void CommandTimeout_CanBeSet()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var options = new QueryExecutionOptions { CommandTimeout = timeout };

        options.CommandTimeout.Should().Be(timeout);
    }

    [Fact(DisplayName = "Command Timeout Default Is Null")]
    public void CommandTimeout_DefaultIsNull()
    {
        var options = new QueryExecutionOptions();

        options.CommandTimeout.Should().BeNull();
    }

    [Fact(DisplayName = "Default Instance Is Shared")]
    public void DefaultInstance_IsShared()
    {
        var options1 = QueryExecutionOptions.Default;
        var options2 = QueryExecutionOptions.Default;

        options1.Should().BeSameAs(options2);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var includes = new[] { "Orders", "Items" };
        var o1 = new QueryExecutionOptions
        {
            IncludeSoftDeleted = true,
            Distinct = true,
            AsNoTracking = false,
            AsSplitQuery = true,
            Includes = includes,
            MaxRows = 500,
            CommandTimeout = TimeSpan.FromMinutes(2)
        };
        var o2 = new QueryExecutionOptions
        {
            IncludeSoftDeleted = true,
            Distinct = true,
            AsNoTracking = false,
            AsSplitQuery = true,
            Includes = includes,
            MaxRows = 500,
            CommandTimeout = TimeSpan.FromMinutes(2)
        };

        o1.Should().BeEquivalentTo(o2);
        o1.Equals(o2).Should().BeTrue();
    }

    [Fact(DisplayName = "Record Equality Different Values Are Not Equal")]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var o1 = new QueryExecutionOptions { IncludeSoftDeleted = true };
        var o2 = new QueryExecutionOptions { IncludeSoftDeleted = false };

        o1.Should().NotBe(o2);
    }

    [Fact(DisplayName = "Includes With Empty Array Should Be Empty")]
    public void Includes_WithEmptyArray_ShouldBeEmpty()
    {
        var options = new QueryExecutionOptions { Includes = Array.Empty<string>() };

        options.Includes.Should().BeEmpty();
    }
}