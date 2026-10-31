using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Contributes module aggregates and projections to the event store.</summary>
public interface IEventStoreModule
{
    /// <summary>Registers the module's factories and projections.</summary>
    /// <param name="services">The host service collection.</param>
    void Configure(IServiceCollection services);
}
