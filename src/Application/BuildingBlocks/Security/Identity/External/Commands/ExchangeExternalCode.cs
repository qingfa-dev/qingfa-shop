using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.Auth.Outcome;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Models;

namespace Application.BuildingBlocks.Security.Identity.External.Commands;

public static class ExchangeExternalCode
{
    #region UseCase
    public sealed record UseCase(ExternalExchangeRequest? Request)
        : ICommand<AuthTokenResponse>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IExternalCodeStore codes)
        : ICommandHandler<UseCase, AuthTokenResponse>
    {
        public async Task<Result<AuthTokenResponse>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            // Validate: the store throws on a blank code, so reject it as a failed exchange.
            if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.Code))
            {
                return Result<AuthTokenResponse>.Fail(IdentityOutcome.Failure.ExternalCodeInvalid);
            }

            return await codes
                .ConsumeAsync(command.Request.Code, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
