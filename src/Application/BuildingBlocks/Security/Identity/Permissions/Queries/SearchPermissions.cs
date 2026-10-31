using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Permissions.Mappings;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Queries;

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

            return snapshot.Bind(value => value.ToPage(query.Parameters));
        }
    }
    #endregion
}
