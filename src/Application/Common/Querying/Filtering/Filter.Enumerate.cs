namespace Application.Common.Querying.Filtering;

/// <summary>The comparison applied by a filter condition.</summary>
public enum FilterOperator
{
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    NotStartsWith,
    EndsWith,
    NotEndsWith,
    Custom,
}


/// <summary>How the children of a filter group combine.</summary>
public enum FilterLogic
{
    And,
    Or,
}