using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Infrastructure.BuildingBlocks.Adapters.Notifications;
using Application.BuildingBlocks.Ports.Notifications.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class NotificationSettingExtensionTests
{
    private static IServiceCollection CreateServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>());
        builder.AddNotificationSetting();
        return builder.Services;
    }

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterIOptions()
    {
        IServiceCollection services = CreateServices();
        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<NotificationSetting>? options = provider.GetService<IOptions<NotificationSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IValidator<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterValidator()
    {
        IServiceCollection services = CreateServices();
        ServiceProvider provider = services.BuildServiceProvider();

        IValidator<NotificationSetting>? validator = provider.GetService<IValidator<NotificationSetting>>();

        validator.Should().NotBeNull();
        validator.Should().BeOfType<NotificationSettingValidator>();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IValidateOptions<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterValidateOptions()
    {
        IServiceCollection services = CreateServices();
        ServiceProvider provider = services.BuildServiceProvider();

        IEnumerable<IValidateOptions<NotificationSetting>> validators =
            provider.GetServices<IValidateOptions<NotificationSetting>>();

        validators.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "AddNotificationSetting should resolve with default values")]
    public void AddNotificationSetting_ShouldResolveDefaultValues()
    {
        IServiceCollection services = CreateServices();
        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<NotificationSetting> options = provider.GetRequiredService<IOptions<NotificationSetting>>();

        options.Value.ApplicationName.Should().Be("ReSys Shop");
    }
}
