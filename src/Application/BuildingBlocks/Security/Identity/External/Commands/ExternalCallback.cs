using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Mappings;
using Application.BuildingBlocks.Security.Identity.External.Models;
using Application.BuildingBlocks.Security.Identity.External.Options;

namespace Application.BuildingBlocks.Security.Identity.External.Commands;

public static class ExternalCallback
{
    #region Request
    public sealed record Request : ExternalCallbackRequestModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<ExternalCallbackModel>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IExternalAuthenticationService external,
        IExternalCodeStore codes,
        ExternalAuthOption option) : ICommandHandler<UseCase, ExternalCallbackModel>
    {
        public async Task<Result<ExternalCallbackModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result<AuthTokenResponse> token = await external
                .LoginAsync(command.Request.ToUserInfo(), cancellationToken)
                .ConfigureAwait(false);

            if (token.IsFailure)
                return Result<ExternalCallbackModel>.Fail(token.Errors, token.StatusCode);

            Result<string> code = await codes
                .StoreAsync(token.Value, cancellationToken)
                .ConfigureAwait(false);

            return code.Map(issued => ExternalMapper.ToCallback(
                option.SuccessRedirectUrl,
                issued));
        }
    }
    #endregion
}
