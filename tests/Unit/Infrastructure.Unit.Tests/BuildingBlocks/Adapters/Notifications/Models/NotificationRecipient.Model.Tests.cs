namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationRecipientModelTests
{
    [Fact(DisplayName = "Create with identifier should return recipient")]
    public void Create_WithIdentifier_ShouldReturnRecipient()
    {
        NotificationRecipient recipient = NotificationRecipient.Create("user@example.com");

        recipient.Identifier.Should().Be("user@example.com");
        recipient.Name.Should().BeNull();
    }

    [Fact(DisplayName = "Create with identifier and name should return recipient")]
    public void Create_WithIdentifierAndName_ShouldReturnRecipient()
    {
        NotificationRecipient recipient = NotificationRecipient.Create("user@example.com", "Alice");

        recipient.Identifier.Should().Be("user@example.com");
        recipient.Name.Should().Be("Alice");
    }

    [Fact(DisplayName = "Equal instances should be equal")]
    public void EqualInstances_ShouldBeEqual()
    {
        NotificationRecipient a = NotificationRecipient.Create("test@test.com", "Test");
        NotificationRecipient b = NotificationRecipient.Create("test@test.com", "Test");

        a.Should().Be(b);
    }
}
