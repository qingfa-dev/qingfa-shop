using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Abstractions;
using Application.Features.Identity.External.Models;

namespace Application.Features.Identity.External.Commands;

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
                return Result<AuthTokenResponse>.Fail(Error.Unauthorized(
                    "Auth.External.CodeInvalid",
                    "The external sign-in code is invalid or expired."));
            }

            return await codes
                .ConsumeAsync(command.Request.Code, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
