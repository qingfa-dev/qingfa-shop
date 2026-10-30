using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class RefreshToken
{
    #region Request
    public sealed record Request(string RefreshToken);
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
            RuleFor(command => command.Request.RefreshToken)
                .ApplyTokenRules();
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

            Result<ApplicationIdentity.AuthResponse> result = await identity
                .RefreshAsync(
                    new ApplicationIdentity.RefreshRequest(command.Request.RefreshToken),
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
