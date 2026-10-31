using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Queries;

public static class GetRolePermissions
{
    #region UseCase
    public sealed record UseCase(Guid RoleId) : IQuery<IReadOnlyList<string>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IPermissionCatalogService catalog)
        : IQueryHandler<UseCase, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await catalog
                .GetRolePermissionsAsync(query.RoleId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
