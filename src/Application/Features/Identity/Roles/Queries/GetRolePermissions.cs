using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Features.Identity.Roles.Queries;

public static class GetRolePermissions
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<IReadOnlyList<string>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : IQueryHandler<UseCase, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            Result<RoleModel> role = await roles
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);

            return role.Map(model => model.Permissions);
        }
    }
    #endregion
}
