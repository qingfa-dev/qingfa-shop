namespace Application.Common.Querying.Filtering;

/// <summary>The comparison applied by a filter condition.</summary>
public enum FilterOperator
{
    /// <summary>Exact equality comparison (field = value).</summary>
    Equal,

    /// <summary>Not-equal comparison (field != value).</summary>
    NotEqual,

    /// <summary>Less-than comparison (&lt;); requires a numeric or date field.</summary>
    LessThan,

    /// <summary>Greater-than comparison (&gt;); requires a numeric or date field.</summary>
    GreaterThan,

    /// <summary>Greater-than-or-equal comparison (&gt;=); requires a numeric or date field.</summary>
    GreaterThanOrEqual,

    /// <summary>Less-than-or-equal comparison (&lt;=); requires a numeric or date field.</summary>
    LessThanOrEqual,

    /// <summary>Substring match: the field contains the value anywhere (case-sensitive by default).</summary>
    Contains,

    /// <summary>Negated substring match: the field does not contain the value.</summary>
    NotContains,

    /// <summary>Prefix match: the field starts with the value.</summary>
    StartsWith,

    /// <summary>Negated prefix match: the field does not start with the value.</summary>
    NotStartsWith,

    /// <summary>Suffix match: the field ends with the value.</summary>
    EndsWith,

    /// <summary>Negated suffix match: the field does not end with the value.</summary>
    NotEndsWith,

    /// <summary>A custom operator defined by the adapter (e.g., regex, distance).</summary>
    Custom,
}


/// <summary>How the children of a filter group combine.</summary>
public enum FilterLogic
{
    /// <summary>All children must match (logical AND).</summary>
    And,

    /// <summary>At least one child must match (logical OR).</summary>
    Or,
}