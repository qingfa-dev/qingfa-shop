using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class ResendConfirmation
{
    #region Request
    public sealed record Request(string Email);
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
                .ResendConfirmationEmailAsync(
                    new ApplicationIdentity.ResendConfirmationEmailRequest(
                        command.Request.Email),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
