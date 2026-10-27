namespace Application.Common.Contexts;

/// <summary>
/// Scoped ambient context populated by the host at the start of a request.
/// </summary>
/// <remarks>
/// Registered by <c>AddApplication</c> and shared by handlers within the same
/// scope. A host middleware sets the values from the authenticated principal;
/// tests set them directly. Both interfaces resolve to this single instance.
/// </remarks>
public sealed class AmbientExecutionContext : ITenantContext, IActorContext
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <inheritdoc />
    public string? Actor { get; set; }
}
