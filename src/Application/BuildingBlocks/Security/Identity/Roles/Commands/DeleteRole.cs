using Application.BuildingBlocks.Security.Identity.Abstractions;

namespace Application.BuildingBlocks.Security.Identity.Roles.Commands;

public static class DeleteRole
{
    #region UseCase
    public sealed record UseCase(Guid Id) : ICommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IRoleManagementService roles)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Result result = await roles
                .DeleteAsync(command.Id, cancellationToken)
                .ConfigureAwait(false);

            return result.IsSuccess ? Result.NoContent() : result;
        }
    }
    #endregion
}
