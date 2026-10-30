using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Features.Identity.Roles.Commands;

public static class UpdateRole
{
    #region Request
    public sealed record Request : UpdateRoleRequestModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Guid Id, Request Request) : ICommand<RoleModel>;
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
                .UpdateAsync(command.Id, command.Request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
