using Application.BuildingBlocks.Security.Identity.Abstractions;

using Application.BuildingBlocks.Security.Identity.Users.Models;

namespace Application.BuildingBlocks.Security.Identity.Users.Commands;

public static class ResetUserPassword
{
    #region Request
    public sealed record Request : ResetUserPasswordModel;
    #endregion

    #region UseCase
    /// <summary>Resets a password; <paramref name="Id"/> is the route identifier and always wins.</summary>
    public sealed record UseCase(Guid Id, Request Request) : ICommand;
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
                .ResetPasswordAsync(
                    command.Id,
                    command.Request.NewPassword,
                    cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
