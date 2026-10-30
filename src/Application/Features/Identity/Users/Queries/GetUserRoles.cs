using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Queries;

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
