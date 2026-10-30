using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.Auth.Commands;

public static class Login
{
    #region UseCase
    public sealed record UseCase(LoginRequest Request)
        : ICommand<AuthTokenResponse>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, AuthTokenResponse>
    {
        public async Task<Result<AuthTokenResponse>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return await identity
                .LoginAsync(command.Request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
