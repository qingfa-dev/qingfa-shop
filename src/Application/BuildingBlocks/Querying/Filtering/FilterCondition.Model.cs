namespace Application.BuildingBlocks.Querying.Filtering;

/// <summary>A single field comparison. The value is already converted to the member type.</summary>
/// <param name="Field">The mapped field name being compared (e.g., "Status", "CreatedAt").</param>
/// <param name="Operator">The comparison operator (Equal, Contains, GreaterThan, etc.).</param>
/// <param name="RawValue">The original text value from the request, before type conversion.</param>
/// <param name="Value">The value converted to the field's expected type for comparison.</param>
/// <param name="ValueType">The .NET type of the converted value (e.g., typeof(int), typeof(string)).</param>
/// <param name="CaseInsensitive">True when the comparison should ignore case (applies to string fields).</param>
/// <param name="IndexValue">For indexed fields, the index value used to locate the element in the collection.</param>
/// <param name="CustomOperator">For Custom operators, the operator name string (e.g., "regex", "distance").</param>
public sealed record FilterCondition(
    string Field,
    FilterOperator Operator,
    string? RawValue,
    object? Value,
    Type ValueType,
    bool CaseInsensitive,
    object? IndexValue,
    string? CustomOperator) : FilterNode;