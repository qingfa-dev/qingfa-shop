using Application.Features.Identity.Users.Commands;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Mappings;

/// <summary>Maps admin user requests onto the account-service specs.</summary>
public static class UserMapper
{
    /// <summary>Maps a create request onto the validated creation spec.</summary>
    /// <param name="request">The request to map.</param>
    /// <returns>The creation spec.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public static CreateUserSpec ToSpec(CreateUser.Request request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateUserSpec(
            request.UserName.Trim(),
            request.Email.Trim(),
            request.Password,
            Normalize(request.FirstName),
            Normalize(request.LastName),
            request.EmailConfirmed,
            request.RoleIds);
    }

    /// <summary>Maps an update request onto the partial update spec.</summary>
    /// <param name="request">The request to map.</param>
    /// <returns>The update spec.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public static UpdateUserSpec ToSpec(UpdateUser.Request request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateUserSpec(
            request.Email?.Trim(),
            Normalize(request.FirstName),
            Normalize(request.LastName),
            request.RoleIds);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
