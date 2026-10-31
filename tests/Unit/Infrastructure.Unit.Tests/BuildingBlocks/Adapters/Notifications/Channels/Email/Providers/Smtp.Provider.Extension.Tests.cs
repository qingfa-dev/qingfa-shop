using Microsoft.AspNetCore.Builder;

using Application.BuildingBlocks.Ports.Notifications.Providers;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SmtpProviderExtensionTests
{
    [Fact(DisplayName = "AddEmailSmtpProvider should register SmtpProvider as INotificationProvider")]
    public void AddEmailSmtpProvider_ShouldRegisterProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEmailSmtpProvider();

        builder.Services.Should().Contain(
            s => s.ServiceType == typeof(INotificationProvider)
                 && s.ImplementationType == typeof(SmtpProvider));
    }
}
