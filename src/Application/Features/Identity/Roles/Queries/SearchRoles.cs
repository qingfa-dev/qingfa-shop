using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Features.Identity.Roles.Queries;

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
