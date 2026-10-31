using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Auth.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.BuildingBlocks.Security.Identity.Auth.Queries;

public static class GetTwoFactor
{
    #region UseCase
    public sealed record UseCase : IQuery<TwoFactorInfo>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : IQueryHandler<UseCase, TwoFactorInfo>
    {
        public async Task<Result<TwoFactorInfo>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.GetTwoFactorInfoAsync(userId, ct),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
