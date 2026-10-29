namespace Infrastructure.Unit.Tests.Integrations.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationContentModelTests
{
    [Fact(DisplayName = "Create with subject and body should return content")]
    public void Create_WithSubjectAndBody_ShouldReturnContent()
    {
        NotificationContent content = NotificationContent.Create("Welcome", "Hello!");

        content.Subject.Should().Be("Welcome");
        content.Body.Should().Be("Hello!");
        content.HtmlBody.Should().BeNull();
    }

    [Fact(DisplayName = "Create with htmlBody should populate HtmlBody")]
    public void Create_WithHtmlBody_ShouldPopulateHtmlBody()
    {
        NotificationContent content = NotificationContent.Create("Welcome", "Hello!", "<p>Hello!</p>");

        content.HtmlBody.Should().Be("<p>Hello!</p>");
    }

    [Fact(DisplayName = "Equal instances should be equal")]
    public void EqualInstances_ShouldBeEqual()
    {
        NotificationContent a = NotificationContent.Create("Subj", "Body", "<p>Body</p>");
        NotificationContent b = NotificationContent.Create("Subj", "Body", "<p>Body</p>");

        a.Should().Be(b);
    }

    [Fact(DisplayName = "Different instances should not be equal")]
    public void DifferentInstances_ShouldNotBeEqual()
    {
        NotificationContent a = NotificationContent.Create("Subj", "Body");
        NotificationContent b = NotificationContent.Create("Subj", "Other");

        a.Should().NotBe(b);
    }
}
