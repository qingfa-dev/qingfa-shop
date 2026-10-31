using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.BuildingBlocks.Security.Authentication.Contexts;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Contexts;

/// <summary>
/// Implementation of <see cref="IUserContext"/> that resolves user information from the HTTP context.
/// </summary>
public class CurrentUser(
    IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    private HttpContext? HttpContext => httpContextAccessor.HttpContext;

    /// <inheritdoc/>
    public string? UserId => User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc/>
    public string? UserName => User?.FindFirstValue(JwtRegisteredClaimNames.Name) ?? User?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc/>
    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc/>
    public string? IpAddress => HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc/>
    public string? Device => HttpContext?.Request.Headers.UserAgent.ToString();
}
