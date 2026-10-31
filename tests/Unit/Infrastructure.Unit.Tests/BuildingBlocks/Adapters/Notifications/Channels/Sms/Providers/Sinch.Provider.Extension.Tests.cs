using Microsoft.AspNetCore.Builder;

using Application.BuildingBlocks.Ports.Notifications.Providers;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class SinchProviderExtensionTests
{
    [Fact(DisplayName = "AddSmsSinchProvider should register SinchProvider as INotificationProvider")]
    public void AddSmsSinchProvider_ShouldRegisterProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddSmsSinchProvider();

        builder.Services.Should().Contain(
            s => s.ServiceType == typeof(INotificationProvider)
                 && s.ImplementationType == typeof(SinchProvider));
    }
}
