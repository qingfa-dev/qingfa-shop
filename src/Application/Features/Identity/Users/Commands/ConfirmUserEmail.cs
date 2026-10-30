using Application.Features.Identity.Abstractions;

using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class ConfirmUserEmail
{
    #region Request
    public sealed record Request : UserTargetModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IUserAccountService users)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result result = await users
                .ConfirmEmailAsync(command.Request.Id, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
