using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class ResetPassword
{
    #region Request
    public sealed record Request(string Email, string Token, string NewPassword);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.Email)
                .ApplyEmailRules();

            RuleFor(command => command.Request.Token)
                .ApplyTokenRules();

            RuleFor(command => command.Request.NewPassword)
                .ApplyPasswordRules();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result result = await identity
                .ResetPasswordAsync(
                    new ApplicationIdentity.ResetPasswordRequest(
                        command.Request.Email,
                        command.Request.Token,
                        command.Request.NewPassword),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
