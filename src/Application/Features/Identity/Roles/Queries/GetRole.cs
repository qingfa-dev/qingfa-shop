using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Roles.Models;

namespace Application.Features.Identity.Roles.Queries;

public static class GetRole
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<RoleModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : IQueryHandler<UseCase, RoleModel>
    {
        public async Task<Result<RoleModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await roles
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
