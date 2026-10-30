using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Queries;

public static class SearchUsers
{
    #region Request
    public sealed record Request : SearchUsersModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<QueryPage<UserModel>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IUserAccountService users)
        : IQueryHandler<UseCase, QueryPage<UserModel>>
    {
        public async Task<Result<QueryPage<UserModel>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            return await users
                .SearchAsync(query.Request.Parameters, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
