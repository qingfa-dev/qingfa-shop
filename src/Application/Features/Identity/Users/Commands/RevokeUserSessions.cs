using Application.Features.Identity.Abstractions;

namespace Application.Features.Identity.Users.Commands;

public static class RevokeUserSessions
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
                .RevokeSessionsAsync(command.Id, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
