using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Queries;

public static class SearchUsers
{

    #region UseCase
    public sealed record UseCase(QueryParameters Parameters) : IQuery<QueryPage<UserModel>>;
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
            ArgumentNullException.ThrowIfNull(query.Parameters);

            return await users
                .SearchAsync(query.Parameters, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
