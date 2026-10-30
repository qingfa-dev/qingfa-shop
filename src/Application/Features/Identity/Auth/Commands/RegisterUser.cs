using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.Auth.Commands;

public static class RegisterUser
{
    #region UseCase
    public sealed record UseCase(RegisterRequest Request)
        : ICommand<IdentityUserModel>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, IdentityUserModel>
    {
        public async Task<Result<IdentityUserModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return await identity
                .RegisterAsync(command.Request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
