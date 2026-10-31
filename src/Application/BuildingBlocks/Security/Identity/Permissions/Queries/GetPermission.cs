using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Queries;

public static class GetPermission
{
    #region UseCase
    public sealed record UseCase(string Identifier) : IQuery<PermissionItemModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IPermissionCatalogService catalog)
        : IQueryHandler<UseCase, PermissionItemModel>
    {
        public async Task<Result<PermissionItemModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await catalog
                .GetAsync(query.Identifier, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
