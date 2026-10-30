using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Features.Identity.Roles.Commands;

public static class RemovePermissionFromRole
{
    #region Request
    public sealed record Request : RolePermissionModel;
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

            return await roles
                .RemovePermissionAsync(
                    command.Request.RoleId,
                    command.Request.Permission,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
