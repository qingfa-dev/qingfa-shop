using Application.BuildingBlocks.Security.Identity.Auth.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.BuildingBlocks.Security.Identity.Auth.Commands;

public static class RefreshToken
{
    #region UseCase
    public sealed record UseCase(RefreshRequest Request)
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
                .RefreshAsync(command.Request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
