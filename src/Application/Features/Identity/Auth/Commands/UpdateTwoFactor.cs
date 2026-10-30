using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Mappings;
using Application.Features.Identity.Auth.Models;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class UpdateTwoFactor
{
    #region Request
    public sealed record Request(
        bool? Enable = null,
        string? TwoFactorCode = null,
        bool? ResetSharedKey = null,
        bool? ResetRecoveryCodes = null);
    #endregion

    #region Response
    public sealed record Response : AuthTwoFactorModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.TwoFactorCode)
                .NotEmpty()
                .When(command => command.Request.Enable is true);
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IIdentityService identity,
        IUserContext userContext)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Request request = command.Request;

            return await AuthGuard.RequireUserId(userContext)
                .BindAsync(
                    (userId, ct) => identity.UpdateTwoFactorAsync(
                        userId,
                        new ApplicationIdentity.TwoFactorRequest(
                            request.Enable,
                            request.TwoFactorCode,
                            request.ResetSharedKey,
                            request.ResetRecoveryCodes),
                        ct),
                    cancellationToken)
                .MapAsync(info => AuthMapper.ToTwoFactorModel<Response>(info));
        }
    }
    #endregion
}
