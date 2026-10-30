using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class RemoveRoleFromUser
{
    #region Request
    public sealed record Request : UserRoleModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<UserModel>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IUserAccountService users)
        : ICommandHandler<UseCase, UserModel>
    {
        public async Task<Result<UserModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return await users
                .RemoveRoleAsync(
                    command.Request.UserId,
                    command.Request.RoleId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
