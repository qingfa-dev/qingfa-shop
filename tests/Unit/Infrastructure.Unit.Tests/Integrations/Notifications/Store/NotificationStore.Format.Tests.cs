namespace Infrastructure.Unit.Tests.Integrations.Notifications.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationStoreFormatTests
{
    [Fact(DisplayName = "Formats should contain all NotificationFormat values")]
    public void Formats_ShouldContainAllNotificationFormatValues()
    {
        NotificationFormat[] formats = Enum.GetValues<NotificationFormat>();

        foreach (NotificationFormat format in formats)
        {
            NotificationStore.Formats.Should().ContainKey(format);
        }
    }
}
