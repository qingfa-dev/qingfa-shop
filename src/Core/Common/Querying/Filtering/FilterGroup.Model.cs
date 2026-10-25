namespace Core.Common.Querying.Filtering;

/// <summary>A logical group of filter nodes.</summary>
public sealed record FilterGroup(
    FilterLogic Logic,
    IReadOnlyList<FilterNode> Children) : FilterNode;