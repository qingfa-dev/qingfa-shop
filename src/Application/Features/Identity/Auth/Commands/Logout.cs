using Application.Common.Security.Authentication.Identity;
using System.Globalization;
using System.Security.Claims;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class Logout
{
    private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    #region Request
    public sealed record Request(
        string? RefreshToken = null,
        string? AccessTokenJti = null,
        DateTimeOffset? AccessTokenExpiresAtUtc = null);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request).NotNull();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Request request = command.Request;

            Result result = await identity
                .LogoutAsync(
                    new ApplicationIdentity.LogoutRequest(
                        request.RefreshToken,
                        request.AccessTokenJti,
                        request.AccessTokenExpiresAtUtc),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion

    #region Helpers
    /// <summary>Builds the command request from the body and the authenticated principal.</summary>
    /// <param name="request">The optional request body.</param>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The request enriched with the access-token claims.</returns>
    /// <remarks>
    /// The access-token identifier and expiry are always read from the principal, never
    /// from the body, so a caller cannot blacklist an arbitrary token.
    /// </remarks>
    public static Request FromPrincipal(Request? request, ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return new Request(
            request?.RefreshToken,
            principal.FindFirstValue("jti"),
            ReadAccessTokenExpiry(principal));
    }

    /// <summary>Reads the access-token expiry from the standard <c>exp</c> claim.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The expiry instant, or <c>null</c> when the claim is absent or invalid.</returns>
    public static DateTimeOffset? ReadAccessTokenExpiry(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? expiresAt = principal.FindFirstValue("exp");

        if (!long.TryParse(
            expiresAt,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long seconds))
        {
            return null;
        }

        return seconds >= 0 && seconds <= MaxUnixSeconds
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
    #endregion
}
