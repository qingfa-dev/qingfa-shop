namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationMetadataModelTests
{
    [Fact(DisplayName = "Priority key should be camelCase")]
    public void Priority_ShouldBeCamelCase()
    {
        NotificationMetadata.Priority.Should().Be("priority");
    }

    [Fact(DisplayName = "Language key should be camelCase")]
    public void Language_ShouldBeCamelCase()
    {
        NotificationMetadata.Language.Should().Be("language");
    }

    [Fact(DisplayName = "CreatedBy key should be camelCase")]
    public void CreatedBy_ShouldBeCamelCase()
    {
        NotificationMetadata.CreatedBy.Should().Be("createdBy");
    }
}
