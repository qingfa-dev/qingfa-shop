namespace Infrastructure.Unit.Tests.Integrations.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationAttachmentModelTests
{
    [Fact(DisplayName = "Create should return attachment with matching properties")]
    public void Create_ShouldReturnAttachmentWithMatchingProperties()
    {
        byte[] data = [0x01, 0x02, 0x03];
        NotificationAttachment attachment = NotificationAttachment.Create("report.pdf", data, "application/pdf");

        attachment.FileName.Should().Be("report.pdf");
        attachment.Data.Should().BeSameAs(data);
        attachment.ContentType.Should().Be("application/pdf");
    }
}
