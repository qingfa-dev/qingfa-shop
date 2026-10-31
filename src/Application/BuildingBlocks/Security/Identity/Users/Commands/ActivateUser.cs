using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Users.Models;

namespace Application.BuildingBlocks.Security.Identity.Users.Commands;

public static class ActivateUser
{
    #region UseCase
    public sealed record UseCase(Guid Id) : ICommand<UserModel>;
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

            return await users
                .SetActiveAsync(command.Id, active: true, ct: cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
