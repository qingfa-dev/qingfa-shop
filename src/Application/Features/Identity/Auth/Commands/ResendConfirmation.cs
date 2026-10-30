using Application.Features.Identity.Auth.Abstractions;
using Application.Features.Identity.Auth.Models;

namespace Application.Features.Identity.Auth.Commands;

public static class ResendConfirmation
{
    #region UseCase
    public sealed record UseCase(ResendConfirmationEmailRequest Request)
        : ICommand, INonTransactionalCommand;
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
                .ResendConfirmationEmailAsync(command.Request, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
