using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.BuildingBlocks.Security.Identity.Roles.Queries;

public static class SearchRoles
{
    #region UseCase
    public sealed record UseCase(QueryParameters Parameters) : IQuery<QueryPage<RoleModel>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : IQueryHandler<UseCase, QueryPage<RoleModel>>
    {
        public async Task<Result<QueryPage<RoleModel>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Parameters);

            return await roles
                .SearchAsync(query.Parameters, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
