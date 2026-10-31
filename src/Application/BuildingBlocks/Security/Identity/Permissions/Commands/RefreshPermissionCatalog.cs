using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Commands;

public static class RefreshPermissionCatalog
{
    #region UseCase
    public sealed record UseCase : ICommand<PermissionSnapshotModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IPermissionCatalogService catalog)
        : ICommandHandler<UseCase, PermissionSnapshotModel>
    {
        public async Task<Result<PermissionSnapshotModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            return await catalog
                .RefreshAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
