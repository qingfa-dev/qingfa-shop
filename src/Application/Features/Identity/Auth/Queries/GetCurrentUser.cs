using Application.Common.Security.Authentication.Identity;
using Application.Common.Mediators.Queries;
using Application.Features.Identity.Auth.Mappings;
using Application.Features.Identity.Auth.Models;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Queries;

public static class GetCurrentUser
{
    #region Request
    public sealed record Request;
    #endregion

    #region Response
    public sealed record Response : AuthUserModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : IQueryHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.GetInfoAsync(userId, ct),
                    cancellationToken)
                .MapAsync(user => AuthMapper.ToUserModel<Response>(user));
        }
    }
    #endregion
}
