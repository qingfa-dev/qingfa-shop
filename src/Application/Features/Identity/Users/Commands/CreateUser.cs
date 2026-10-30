using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Mappings;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class CreateUser
{
    #region Request
    public sealed record Request : CreateUserModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<UserModel>;
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

            Result<UserModel> created = await users
                .CreateAsync(UserMapper.ToSpec(command.Request), cancellationToken)
                .ConfigureAwait(false);

            return created.IsSuccess
                ? created.WithStatus(ResultConstant.StatusCode.Created)
                : created;
        }
    }
    #endregion
}
