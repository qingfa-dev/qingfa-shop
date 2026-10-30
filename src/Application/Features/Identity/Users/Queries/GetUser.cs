using Application.Common.Mediators.Queries;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Queries;

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
