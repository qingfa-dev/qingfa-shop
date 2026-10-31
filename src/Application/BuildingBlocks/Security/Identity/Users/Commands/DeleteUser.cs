using Application.BuildingBlocks.Security.Identity.Abstractions;

namespace Application.BuildingBlocks.Security.Identity.Users.Commands;

public static class DeleteUser
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
                .DeleteAsync(command.Id, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
