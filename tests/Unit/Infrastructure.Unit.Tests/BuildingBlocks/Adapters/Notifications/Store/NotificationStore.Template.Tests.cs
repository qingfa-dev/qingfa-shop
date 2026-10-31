using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationStoreTemplateTests
{
    [Fact(DisplayName = "Templates should contain an entry for each NotificationUseCase except None")]
    public void Templates_ShouldContainAllUseCases()
    {
        NotificationUseCase[] useCases = Enum.GetValues<NotificationUseCase>();

        foreach (NotificationUseCase useCase in useCases)
        {
            if (useCase == NotificationUseCase.None)
            {
                continue;
            }

            NotificationStore.Templates.Should().ContainKey(useCase);
        }
    }

    [Fact(DisplayName = "Templates should have non-null TemplateContent and HtmlTemplateContent when format requires it")]
    public void Templates_ShouldHaveNonNullContent()
    {
        foreach (KeyValuePair<NotificationUseCase, NotificationTemplate> entry in NotificationStore.Templates)
        {
            entry.Value.TemplateContent.Should().NotBeNull();
            if (entry.Value.TemplateFormatType == NotificationFormat.Html)
            {
                entry.Value.HtmlTemplateContent.Should().NotBeNull();
            }
        }
    }

    [Fact(DisplayName = "Templates should have a valid SendMethodType when not None")]
    public void Templates_ShouldHaveValidChannel()
    {
        foreach (KeyValuePair<NotificationUseCase, NotificationTemplate> entry in NotificationStore.Templates)
        {
            if (entry.Value.SendMethodType != NotificationChannel.None)
            {
                entry.Value.SendMethodType.Should().BeOneOf(
                    NotificationChannel.Email,
                    NotificationChannel.SMS,
                    NotificationChannel.PushNotification,
                    NotificationChannel.WhatsApp);
            }
        }
    }
}
