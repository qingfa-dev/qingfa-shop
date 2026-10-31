using Application.BuildingBlocks.Security.Identity.Auth.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.BuildingBlocks.Security.Identity.Auth.Commands;

public static class UpdateCurrentUser
{
    #region UseCase
    public sealed record UseCase(UpdateInfoRequest Request)
        : ICommand<IdentityUserModel>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : ICommandHandler<UseCase, IdentityUserModel>
    {
        public async Task<Result<IdentityUserModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.UpdateInfoAsync(userId, command.Request, ct),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
