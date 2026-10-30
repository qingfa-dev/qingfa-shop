using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Permissions.Mappings;
using Application.Features.Identity.Permissions.Models;

namespace Application.Features.Identity.Permissions.Queries;

public static class SearchPermissions
{
    #region UseCase
    public sealed record UseCase(QueryParameters Parameters)
        : IQuery<QueryPage<PermissionItemModel>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IPermissionCatalogService catalog)
        : IQueryHandler<UseCase, QueryPage<PermissionItemModel>>
    {
        public async Task<Result<QueryPage<PermissionItemModel>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Parameters);

            Result<PermissionSnapshotModel> snapshot = await catalog
                .SearchAsync(query.Parameters, cancellationToken)
                .ConfigureAwait(false);

            return snapshot.Map(value => value.ToPage(query.Parameters));
        }
    }
    #endregion
}
