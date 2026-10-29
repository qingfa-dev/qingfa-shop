using Application.Common.Integrations.Notifications.Templates;

namespace Infrastructure.Unit.Tests.Integrations.Notifications.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationStorePriorityLevelTests
{
    [Fact(DisplayName = "PriorityLevels should contain all NotificationPriorityLevel values")]
    public void PriorityLevels_ShouldContainAllLevels()
    {
        NotificationPriorityLevel[] levels = Enum.GetValues<NotificationPriorityLevel>();

        foreach (NotificationPriorityLevel level in levels)
        {
            NotificationStore.PriorityLevels.Should().ContainKey(level);
        }
    }

    [Fact(DisplayName = "PriorityLevels should have valid priorities")]
    public void PriorityLevels_ShouldHaveValidPriorities()
    {
        NotificationPriorityLevel[] definedLevels = Enum.GetValues<NotificationPriorityLevel>();

        foreach (KeyValuePair<NotificationPriorityLevel, NotificationDefinition<NotificationPriorityLevel>> entry in NotificationStore.PriorityLevels)
        {
            definedLevels.Should().Contain(entry.Value.Value);
        }
    }
}
