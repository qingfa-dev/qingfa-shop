using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Mappings;
using Application.Features.Identity.Auth.Models;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class Login
{
    #region Request
    public sealed record Request(
        string Identifier,
        string Password,
        string? TwoFactorCode = null,
        string? TwoFactorRecoveryCode = null);
    #endregion

    #region Response
    public sealed record Response(
        string AccessToken,
        DateTimeOffset ExpiresAtUtc,
        string RefreshToken,
        ApplicationIdentity.IdentityUserModel User)
        : AuthTokenModel(AccessToken, ExpiresAtUtc, RefreshToken, User);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.Identifier).NotEmpty();
            RuleFor(command => command.Request.Password).NotEmpty();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Request request = command.Request;

            Result<ApplicationIdentity.AuthResponse> result = await identity
                .LoginAsync(
                    new ApplicationIdentity.LoginRequest(
                        request.Identifier,
                        request.Password,
                        request.TwoFactorCode,
                        request.TwoFactorRecoveryCode),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.Map(token => new Response(
                token.AccessToken.Token,
                token.AccessToken.ExpiresAtUtc,
                token.RefreshToken.Token,
                token.User));
        }
    }
    #endregion
}
