using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationMessageModelTests
{
    [Fact(DisplayName = "Create with all params should return message")]
    public void Create_WithAllParams_ShouldReturnMessage()
    {
        NotificationRecipient recipient = NotificationRecipient.Create("user@test.com");
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "ReSys"));
        NotificationAttachment attachment = NotificationAttachment.Create("file.pdf", [], "application/pdf");

        NotificationMessage message = NotificationMessage.Create(
            NotificationUseCase.UserRegistered,
            recipient,
            NotificationChannel.Email,
            context,
            [attachment],
            (NotificationMetadata.Priority, NotificationPriorityLevel.High));

        message.UseCase.Should().Be(NotificationUseCase.UserRegistered);
        message.Recipient.Should().Be(recipient);
        message.Channel.Should().Be(NotificationChannel.Email);
        message.Context.Should().Be(context);
        message.Attachments.Should().ContainSingle().Which.Should().Be(attachment);
        message.Metadata[NotificationMetadata.Priority].Should().Be(NotificationPriorityLevel.High);
    }

    [Fact(DisplayName = "Create without attachments and metadata should have null and empty dictionary")]
    public void Create_WithoutAttachmentsAndMetadata_ShouldHaveNullAndEmptyDictionary()
    {
        NotificationRecipient recipient = NotificationRecipient.Create("user@test.com");
        NotificationContext context = NotificationContext.Empty;

        NotificationMessage message = NotificationMessage.Create(
            NotificationUseCase.OrderConfirmed,
            recipient,
            NotificationChannel.SMS,
            context);

        message.Attachments.Should().BeNull();
        message.Metadata.Should().BeEmpty();
    }
}
