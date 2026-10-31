using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Registers the Sinch SMS notification provider with the DI container.</summary>
public static class SinchProviderExtensions
{
    /// <summary>Adds the Sinch SMS provider as a scoped INotificationProvider service.</summary>
    public static WebApplicationBuilder AddSmsSinchProvider(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddScoped<INotificationProvider, SinchProvider>();
        return builder;
    }
}
