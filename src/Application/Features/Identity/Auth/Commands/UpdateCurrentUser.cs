using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Mappings;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class UpdateCurrentUser
{
    #region Request
    public sealed record Request(
        string? Email = null,
        string? NewPassword = null,
        string? OldPassword = null,
        string? FirstName = null,
        string? LastName = null);
    #endregion

    #region Response
    public sealed record Response : AuthUserModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.Email)
                .ApplyOptionalEmailRules()
                .When(command => command.Request.Email is not null);

            RuleFor(command => command.Request.FirstName)
                .MaximumLength(ApplicationIdentity.IdentityConstant.Constraints.MaxFirstNameLength)
                .When(command => command.Request.FirstName is not null);

            RuleFor(command => command.Request.LastName)
                .MaximumLength(ApplicationIdentity.IdentityConstant.Constraints.MaxLastNameLength)
                .When(command => command.Request.LastName is not null);

            RuleFor(command => command.Request.NewPassword)
                .ApplyOptionalPasswordRules()
                .When(command => command.Request.NewPassword is not null);

            RuleFor(command => command.Request.OldPassword)
                .NotEmpty()
                .When(command => command.Request.NewPassword is not null);
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
                    (userId, ct) => identity.UpdateInfoAsync(
                        userId,
                        new ApplicationIdentity.UpdateInfoRequest(
                            request.Email,
                            request.NewPassword,
                            request.OldPassword,
                            request.FirstName,
                            request.LastName),
                        ct),
                    cancellationToken)
                .MapAsync(user => AuthMapper.ToUserModel<Response>(user));
        }
    }
    #endregion
}
