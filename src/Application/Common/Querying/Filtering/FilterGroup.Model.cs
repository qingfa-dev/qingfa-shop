namespace Application.Common.Querying.Filtering;

/// <summary>A logical group of filter nodes.</summary>
/// <param name="Logic">How children are combined: And (all must match) or Or (at least one must match).</param>
/// <param name="Children">Child nodes, which may be conditions or nested groups.</param>
public sealed record FilterGroup(
    FilterLogic Logic,
    IReadOnlyList<FilterNode> Children) : FilterNode;