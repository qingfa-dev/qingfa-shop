using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Permissions.Models;

namespace Application.Features.Identity.Permissions.Queries;

public static class CheckUserPermissions
{
    #region Request
    public sealed record Request : CheckUserPermissionsRequestModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Guid UserId, Request Request) : IQuery<PermissionCheckModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IPermissionCatalogService catalog)
        : IQueryHandler<UseCase, PermissionCheckModel>
    {
        public async Task<Result<PermissionCheckModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            Result<bool> granted = await catalog
                .CheckUserPermissionsAsync(
                    query.UserId,
                    query.Request.Permissions,
                    query.Request.Mode,
                    cancellationToken)
                .ConfigureAwait(false);

            return granted.Map(value => new PermissionCheckModel
            {
                UserId = query.UserId,
                Permissions = query.Request.Permissions,
                Mode = query.Request.Mode,
                Granted = value,
            });
        }
    }
    #endregion
}
