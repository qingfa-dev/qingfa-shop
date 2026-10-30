using Application.Common.Mediators.Queries;
using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.Auth.Queries;

public static class GetCurrentUser
{
    #region UseCase
    public sealed record UseCase : IQuery<IdentityUserModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : IQueryHandler<UseCase, IdentityUserModel>
    {
        public async Task<Result<IdentityUserModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.GetInfoAsync(userId, ct),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
