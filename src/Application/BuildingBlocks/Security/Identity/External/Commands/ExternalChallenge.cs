using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Mappings;
using Application.BuildingBlocks.Security.Identity.External.Models;
using Application.BuildingBlocks.Security.Identity.External.Options;

namespace Application.BuildingBlocks.Security.Identity.External.Commands;

public static class ExternalChallenge
{
    #region Request
    public sealed record Request : ExternalChallengeRequestModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<ExternalChallengeModel>, INonTransactionalCommand;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IExternalAuthenticationService external,
        ExternalAuthOption option) : ICommandHandler<UseCase, ExternalChallengeModel>
    {
        public Task<Result<ExternalChallengeModel>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result<ExternalChallengeModel> challenge = external
                .GetProviders()
                .ResolveProvider(command.Request.Provider)
                .Map(provider => provider.ToChallenge(option));

            return Task.FromResult(challenge);
        }
    }
    #endregion
}
