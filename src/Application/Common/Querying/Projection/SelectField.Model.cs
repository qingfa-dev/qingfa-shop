namespace Application.Common.Querying.Projection;

/// <summary>One projected column.</summary>
public sealed record SelectField(string Field, string Alias);