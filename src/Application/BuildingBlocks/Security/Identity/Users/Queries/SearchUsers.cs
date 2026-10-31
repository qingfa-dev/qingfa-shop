using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Users.Models;

namespace Application.BuildingBlocks.Security.Identity.Users.Queries;

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
