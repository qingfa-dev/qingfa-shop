using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Permissions.Models;

namespace Application.Features.Identity.Permissions.Queries;

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
