using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Mappings;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class UpdateUser
{
    #region Request
    public sealed record Request : UpdateUserModel;
    #endregion

    #region UseCase
    /// <summary>Updates a user; <paramref name="Id"/> is the route identifier and always wins.</summary>
    public sealed record UseCase(Guid Id, Request Request) : ICommand<UserModel>;
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
                .UpdateAsync(
                    command.Id,
                    UserMapper.ToSpec(command.Request),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
