using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Users.Models;

namespace Application.BuildingBlocks.Security.Identity.Users.Queries;

public static class GetUser
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<UserModel>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IUserAccountService users)
        : IQueryHandler<UseCase, UserModel>
    {
        public async Task<Result<UserModel>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await users
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
