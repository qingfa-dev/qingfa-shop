namespace Core.Common.Querying.Filtering;

/// <summary>A single field comparison. The value is already converted to the member type.</summary>
public sealed record FilterCondition(
    string Field,
    FilterOperator Operator,
    string? RawValue,
    object? Value,
    Type ValueType,
    bool CaseInsensitive,
    object? IndexValue,
    string? CustomOperator) : FilterNode;