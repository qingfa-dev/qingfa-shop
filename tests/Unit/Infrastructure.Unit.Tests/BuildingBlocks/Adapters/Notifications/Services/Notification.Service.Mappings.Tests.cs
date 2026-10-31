using Application.BuildingBlocks.Ports.Notifications.Options;
using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationMapperTests
{
    private static NotificationMessage CreateMessage(NotificationUseCase useCase)
    {
        return NotificationMessage.Create(
            useCase,
            NotificationRecipient.Create("user@test.com"),
            NotificationChannel.Email,
            NotificationContext.Empty);
    }

    [Fact(DisplayName = "MapContent with valid message should return rendered content")]
    public void MapContent_WithValidMessage_ShouldReturnRenderedContent()
    {
        // Arrange: message with an existing template in the store
        NotificationMessage message = CreateMessage(NotificationUseCase.UserRegistered);

        // Act
        Result<NotificationContent> result = message.MapContent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().NotBeNullOrEmpty();
        result.Value.Body.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "MapContent with unknown use case should return TemplateNotFound")]
    public void MapContent_WithUnknownUseCase_ShouldReturnTemplateNotFound()
    {
        // Arrange: message with a use case not in the template store
        NotificationMessage message = CreateMessage(NotificationUseCase.None);

        // Act
        Result<NotificationContent> result = message.MapContent();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Notification.TemplateNotFound");
    }

    [Fact(DisplayName = "ApplyDefaults with missing ApplicationName should fill default")]
    public void ApplyDefaults_WithMissingApplicationName_ShouldFillDefault()
    {
        // Arrange: message without ApplicationName param, options have ApplicationName value
        NotificationMessage message = CreateMessage(NotificationUseCase.UserRegistered);
        NotificationSetting defaults = new() { ApplicationName = "TestStore" };

        // Act
        NotificationMessage result = message.ApplyDefaults(defaults);

        // Assert
        result.Context.GetValue(NotificationParameterType.ApplicationName).Should().Be("TestStore");
    }

    [Fact(DisplayName = "ApplyDefaults with existing values should not overwrite")]
    public void ApplyDefaults_WithExistingValues_ShouldNotOverwrite()
    {
        // Arrange: message with ApplicationName already set, options have different value
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "Custom"));
        NotificationMessage message = NotificationMessage.Create(
            NotificationUseCase.UserRegistered,
            NotificationRecipient.Create("user@test.com"),
            NotificationChannel.Email,
            context);
        NotificationSetting defaults = new() { ApplicationName = "TestStore" };

        // Act
        NotificationMessage result = message.ApplyDefaults(defaults);

        // Assert: original ApplicationName preserved, not overwritten
        result.Context.GetValue(NotificationParameterType.ApplicationName).Should().Be("Custom");
    }

    [Theory(DisplayName = "ToQueueName should map correct queue")]
    [InlineData(NotificationPriorityLevel.Critical, "critical")]
    [InlineData(NotificationPriorityLevel.High, "high")]
    [InlineData(NotificationPriorityLevel.Normal, "default")]
    [InlineData(NotificationPriorityLevel.Low, "low")]
    public void ToQueueName_ShouldMapCorrectQueue(NotificationPriorityLevel priority, string expectedQueue)
    {
        // Act
        string result = priority.ToQueueName();

        // Assert
        result.Should().Be(expectedQueue);
    }
}