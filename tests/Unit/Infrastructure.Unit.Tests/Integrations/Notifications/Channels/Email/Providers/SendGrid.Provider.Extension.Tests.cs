using Microsoft.AspNetCore.Builder;

using Application.Common.Integrations.Notifications.Providers;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Channels.Email.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class SendGridProviderExtensionTests
{
    [Fact(DisplayName = "AddEmailSendGridProvider should register SendGridProvider as INotificationProvider")]
    public void AddEmailSendGridProvider_ShouldRegisterProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEmailSendGridProvider();

        builder.Services.Should().Contain(
            s => s.ServiceType == typeof(INotificationProvider)
                 && s.ImplementationType == typeof(SendGridProvider));
    }
}
