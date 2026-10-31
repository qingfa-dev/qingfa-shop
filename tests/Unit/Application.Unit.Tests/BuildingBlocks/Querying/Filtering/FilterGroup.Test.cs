using Application.BuildingBlocks.Querying.Filtering;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Filtering;


public sealed class FilterGroupTest
{
    [Fact(DisplayName = "Constructor With And Logic And Children Sets Properties")]
    public void Constructor_WithAndLogicAndChildren_SetsProperties()
    {
        var children = new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null),
            new FilterCondition("b", FilterOperator.Equal, "2", "2", typeof(string), false, null, null)
        };
        var group = new FilterGroup(FilterLogic.And, children);

        group.Logic.Should().Be(FilterLogic.And);
        group.Children.Should().Equal(children);
    }

    [Fact(DisplayName = "Constructor With Or Logic And Children Sets Properties")]
    public void Constructor_WithOrLogicAndChildren_SetsProperties()
    {
        var children = new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null)
        };
        var group = new FilterGroup(FilterLogic.Or, children);

        group.Logic.Should().Be(FilterLogic.Or);
        group.Children.Should().Equal(children);
    }

    [Fact(DisplayName = "Constructor With Empty Children Sets Empty List")]
    public void Constructor_WithEmptyChildren_SetsEmptyList()
    {
        var group = new FilterGroup(FilterLogic.And, []);

        group.Children.Should().BeEmpty();
    }

    [Fact(DisplayName = "Constructor With Nested Groups Works")]
    public void Constructor_WithNestedGroups_Works()
    {
        var inner = new FilterGroup(FilterLogic.Or, new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null),
            new FilterCondition("b", FilterOperator.Equal, "2", "2", typeof(string), false, null, null)
        });
        var outer = new FilterGroup(FilterLogic.And, new List<FilterNode>
        {
            inner,
            new FilterCondition("c", FilterOperator.Equal, "3", "3", typeof(string), false, null, null)
        });

        outer.Logic.Should().Be(FilterLogic.And);
        outer.Children.Should().HaveCount(2);
        outer.Children[0].Should().BeSameAs(inner);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var children = new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null)
        };
        var g1 = new FilterGroup(FilterLogic.And, children);
        var g2 = new FilterGroup(FilterLogic.And, children);

        g1.Should().Be(g2);
    }

    [Fact(DisplayName = "Record Equality Different Logic Are Not Equal")]
    public void RecordEquality_DifferentLogic_AreNotEqual()
    {
        var children = new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null)
        };
        var g1 = new FilterGroup(FilterLogic.And, children);
        var g2 = new FilterGroup(FilterLogic.Or, children);

        g1.Should().NotBe(g2);
    }

    [Fact(DisplayName = "Record Equality Different Children Are Not Equal")]
    public void RecordEquality_DifferentChildren_AreNotEqual()
    {
        var g1 = new FilterGroup(FilterLogic.And, new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null)
        });
        var g2 = new FilterGroup(FilterLogic.And, new List<FilterNode>
        {
            new FilterCondition("b", FilterOperator.Equal, "2", "2", typeof(string), false, null, null)
        });

        g1.Should().NotBe(g2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new FilterGroup(FilterLogic.And, []);
        var modified = original with { Logic = FilterLogic.Or };

        modified.Logic.Should().Be(FilterLogic.Or);
        original.Logic.Should().Be(FilterLogic.And);
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var children = new List<FilterNode>
        {
            new FilterCondition("a", FilterOperator.Equal, "1", "1", typeof(string), false, null, null)
        };
        var group = new FilterGroup(FilterLogic.Or, children);
        var (logic, ch) = group;

        logic.Should().Be(FilterLogic.Or);
        ch.Should().Equal(children);
    }

    [Fact(DisplayName = "Inherits From Filter Node")]
    public void InheritsFromFilterNode()
    {
        var group = new FilterGroup(FilterLogic.And, []);

        group.Should().BeAssignableTo<FilterNode>();
    }
}