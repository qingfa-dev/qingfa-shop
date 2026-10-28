using System.Security.Claims;

using Application.Common.Security.Authentication.Contexts;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.Security.Authentication.Contexts;

/// <summary>
/// Implementation of <see cref="ITenantContext"/> that resolves the tenant identifier from the HTTP context.
/// </summary>
public sealed class TenantContext(
    IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid TenantId
    {
        get
        {
            string? tenantId = User?.FindFirstValue("tenant_id")
                ?? User?.FindFirstValue(ClaimTypes.Role);

            return Guid.TryParse(tenantId, out var parsed) ? parsed : Guid.Empty;
        }
    }
}