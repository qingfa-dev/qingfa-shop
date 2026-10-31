using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.BuildingBlocks.Security.Identity.Roles.Queries;

public static class GetRole
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<RoleModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : IQueryHandler<UseCase, RoleModel>
    {
        public async Task<Result<RoleModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await roles
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
