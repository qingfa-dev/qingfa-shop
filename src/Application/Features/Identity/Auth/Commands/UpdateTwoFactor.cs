using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.Auth.Commands;

public static class UpdateTwoFactor
{
    #region UseCase
    public sealed record UseCase(TwoFactorRequest Request)
        : ICommand<TwoFactorInfo>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : ICommandHandler<UseCase, TwoFactorInfo>
    {
        public async Task<Result<TwoFactorInfo>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.UpdateTwoFactorAsync(userId, command.Request, ct),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
