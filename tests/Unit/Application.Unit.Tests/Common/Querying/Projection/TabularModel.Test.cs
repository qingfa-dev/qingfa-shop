using Application.Common.Querying.Projection;

namespace Application.Tests.Common.Querying.Projection;
using Xunit;

public sealed class TabularModelTest
{
    [Fact(DisplayName = "Tabular Column Should Record Name And Type")]
    public void TabularColumn_ShouldRecordNameAndType()
    {
        var column = new TabularColumn("name", typeof(string));

        column.Name.Should().Be("name");
        column.ValueType.Should().Be(typeof(string));
    }

    [Fact(DisplayName = "Tabular Column With Int Type Should Record Correctly")]
    public void TabularColumn_WithIntType_ShouldRecordCorrectly()
    {
        var column = new TabularColumn("score", typeof(int));

        column.Name.Should().Be("score");
        column.ValueType.Should().Be(typeof(int));
    }

    [Fact(DisplayName = "Tabular Row Should Hold Values")]
    public void TabularRow_ShouldHoldValues()
    {
        var row = new TabularRow(new object?[] { "John", 25 });

        row.Values.Should().HaveCount(2);
        row.Values[0].Should().Be("John");
        row.Values[1].Should().Be(25);
    }

    [Fact(DisplayName = "Tabular Row With Null Value Should Hold Null")]
    public void TabularRow_WithNullValue_ShouldHoldNull()
    {
        var row = new TabularRow(new object?[] { "John", null });

        row.Values[1].Should().BeNull();
    }

    [Fact(DisplayName = "Tabular Result Should Hold Columns And Rows")]
    public void TabularResult_ShouldHoldColumnsAndRows()
    {
        var columns = new[]
        {
            new TabularColumn("name", typeof(string)),
            new TabularColumn("score", typeof(int)),
        };
        var rows = new[]
        {
            new TabularRow(new object?[] { "John", 25 }),
            new TabularRow(new object?[] { "Jane", 30 }),
        };
        var result = new TabularResult(columns, rows);

        result.Columns.Should().HaveCount(2);
        result.Rows.Should().HaveCount(2);
        result.Columns[0].Name.Should().Be("name");
        result.Rows[0].Values[0].Should().Be("John");
    }

    [Fact(DisplayName = "Tabular Result With Empty Rows Should Be Valid")]
    public void TabularResult_WithEmptyRows_ShouldBeValid()
    {
        var columns = new[] { new TabularColumn("name", typeof(string)) };
        var rows = Array.Empty<TabularRow>();
        var result = new TabularResult(columns, rows);

        result.Columns.Should().HaveCount(1);
        result.Rows.Should().BeEmpty();
    }
}