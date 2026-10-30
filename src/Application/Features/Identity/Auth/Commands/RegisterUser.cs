using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Mappings;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class RegisterUser
{
    #region Request
    public sealed record Request(
        string UserName,
        string Email,
        string Password,
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
            RuleFor(command => command.Request.UserName)
                .ApplyUserNameRules();

            RuleFor(command => command.Request.Email)
                .ApplyEmailRules();

            RuleFor(command => command.Request.Password)
                .ApplyPasswordRules();

            RuleFor(command => command.Request.FirstName)
                .MaximumLength(ApplicationIdentity.IdentityConstant.Constraints.MaxFirstNameLength)
                .When(command => command.Request.FirstName is not null);

            RuleFor(command => command.Request.LastName)
                .MaximumLength(ApplicationIdentity.IdentityConstant.Constraints.MaxLastNameLength)
                .When(command => command.Request.LastName is not null);
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Request request = command.Request;

            Result<ApplicationIdentity.IdentityUserModel> result = await identity
                .RegisterAsync(
                    new ApplicationIdentity.RegisterRequest(
                        request.UserName,
                        request.Email,
                        request.Password,
                        request.FirstName,
                        request.LastName),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.Map(user => AuthMapper.ToUserModel<Response>(user));
        }
    }
    #endregion
}
