using Application.Features.Identity.Abstractions;

using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Users.Commands;

public static class ConfirmUserEmail
{
    #region UseCase
    public sealed record UseCase(Guid Id) : ICommand;
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

            Result result = await users
                .ConfirmEmailAsync(command.Id, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
