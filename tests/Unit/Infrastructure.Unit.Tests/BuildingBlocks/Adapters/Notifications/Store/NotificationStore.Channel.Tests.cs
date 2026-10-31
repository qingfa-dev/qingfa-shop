using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationStoreChannelTests
{
    [Fact(DisplayName = "SendMethods should contain all NotificationChannel values except None")]
    public void SendMethods_ShouldContainAllChannels()
    {
        NotificationChannel[] channels = Enum.GetValues<NotificationChannel>();

        foreach (NotificationChannel channel in channels)
        {
            if (channel == NotificationChannel.None)
            {
                continue;
            }

            NotificationStore.SendMethods.Should().ContainKey(channel);
        }
    }

    [Fact(DisplayName = "SendMethods should have non-empty channel entries")]
    public void SendMethods_ShouldHaveNonEmptyChannels()
    {
        foreach (KeyValuePair<NotificationChannel, NotificationDefinition<NotificationChannel>> entry in NotificationStore.SendMethods)
        {
            entry.Value.Name.Should().NotBeNullOrEmpty();
            entry.Value.Description.Should().NotBeNullOrEmpty();
        }
    }
}
