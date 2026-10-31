using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Queries;

public static class ListPermissionCategories
{
    #region UseCase
    public sealed record UseCase : IQuery<IReadOnlyList<string>>;
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
                .ListCategoriesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
