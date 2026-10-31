using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Users.Models;

namespace Application.BuildingBlocks.Security.Identity.Users.Queries;

public static class GetUserRoles
{
    #region UseCase
    public sealed record UseCase(Guid Id) : IQuery<IReadOnlyList<string>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IUserAccountService users)
        : IQueryHandler<UseCase, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            Result<UserModel> user = await users
                .GetAsync(query.Id, cancellationToken)
                .ConfigureAwait(false);

            return user.Map(model => model.Roles);
        }
    }
    #endregion
}
