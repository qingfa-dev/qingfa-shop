using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.BuildingBlocks.Security.Identity.Roles.Commands;

public static class CreateRole
{
    #region Request
    public sealed record Request : CreateRoleRequestModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<RoleModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : ICommandHandler<UseCase, RoleModel>
    {
        public async Task<Result<RoleModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result<RoleModel> created = await roles
                .CreateAsync(command.Request, cancellationToken)
                .ConfigureAwait(false);

            return created.IsSuccess
                ? created.WithStatus(ResultConstant.StatusCode.Created)
                : created;
        }
    }
    #endregion
}
