namespace Application.Common.Querying.Projection;

/// <summary>One projected column.</summary>
/// <param name="Field">The mapped field name to project.</param>
/// <param name="Alias">The column alias in the result set; defaults to the field name if not specified.</param>
public sealed record SelectField(
    string Field,
    string Alias);