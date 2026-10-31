using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Models;

namespace Application.BuildingBlocks.Security.Identity.External.Queries;

public static class GetExternalProviders
{
    #region UseCase
    public sealed record UseCase : IQuery<IReadOnlyCollection<ExternalProviderModel>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IExternalAuthenticationService external)
        : IQueryHandler<UseCase, IReadOnlyCollection<ExternalProviderModel>>
    {
        public Task<Result<IReadOnlyCollection<ExternalProviderModel>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return Task.FromResult(
                Result<IReadOnlyCollection<ExternalProviderModel>>.Ok(external.GetProviders()));
        }
    }
    #endregion
}
