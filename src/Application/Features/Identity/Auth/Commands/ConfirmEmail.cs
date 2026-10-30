using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Validators;

using FluentValidation;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Commands;

public static class ConfirmEmail
{
    #region Request
    public sealed record Request(string UserId, string Token);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand, INonTransactionalCommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.UserId)
                .NotEmpty()
                .Must(userId => Guid.TryParse(userId, out _))
                .WithMessage("The user identifier must be a valid GUID.");

            RuleFor(command => command.Request.Token)
                .ApplyTokenRules();
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
                .ConfirmEmailAsync(
                    new ApplicationIdentity.ConfirmEmailRequest(
                        command.Request.UserId,
                        command.Request.Token),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
