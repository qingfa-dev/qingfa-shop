using Application.Features.Identity.External.Abstractions;
using Application.Features.Identity.External.Mappings;
using Application.Features.Identity.External.Models;
using Application.Features.Identity.External.Options;

namespace Application.Features.Identity.External.Commands;

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
