using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class UnlockUser
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
                .SetLockedAsync(command.Id, locked: false, ct: cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
