using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.BuildingBlocks.Security.Identity.Roles.Queries;

public static class GetRolePermissions
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<IReadOnlyList<string>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : IQueryHandler<UseCase, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            Result<RoleModel> role = await roles
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);

            return role.Map(model => model.Permissions);
        }
    }
    #endregion
}
