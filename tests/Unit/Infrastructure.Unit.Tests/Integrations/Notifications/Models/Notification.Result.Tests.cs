namespace Infrastructure.Unit.Tests.Integrations.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationResultTests
{
    public static TheoryData<string, int, string> TemplateNotFoundData => new()
    {
        { "Notification.TemplateNotFound", 404, "UserRegistered" },
        { "Notification.TemplateNotFound", 404, "WelcomeSent" },
    };

    public static TheoryData<string, int, string> UnexpectedData => new()
    {
        { "Notification.Unexpected", 500, "Something went wrong" },
        { "Notification.Unexpected", 500, "Null reference encountered" },
    };

    [Theory(DisplayName = "TemplateNotFound should return error with code, type, and message")]
    [MemberData(nameof(TemplateNotFoundData))]
    public void TemplateNotFound_ShouldReturnError(string expectedCode, int expectedType, string useCase)
    {
        Error error = NotificationResult.Failure.TemplateNotFound(useCase);

        error.Code.Should().Be(expectedCode);
        error.Status.Should().Be(expectedType);
        error.Message.Should().Be($"Template for use case '{useCase}' not found.");
    }

    [Fact(DisplayName = "UnsupportedMethod should return error with code and type")]
    public void UnsupportedMethod_ShouldReturnError()
    {
        Error error = NotificationResult.Failure.UnsupportedMethod;

        error.Code.Should().Be("Notification.UnsupportedMethod");
        error.Status.Should().Be(422);
    }

    [Theory(DisplayName = "Unexpected should return error with code, type, and message")]
    [MemberData(nameof(UnexpectedData))]
    public void Unexpected_ShouldReturnError(string expectedCode, int expectedType, string message)
    {
        Error error = NotificationResult.Failure.Unexpected(message);

        error.Code.Should().Be(expectedCode);
        error.Status.Should().Be(expectedType);
        error.Message.Should().Be(message);
    }

    [Fact(DisplayName = "UseCaseRequired should return error with code and type")]
    public void UseCaseRequired_ShouldReturnError()
    {
        Error error = NotificationResult.Failure.UseCaseRequired;

        error.Code.Should().Be("Notification.UseCaseRequired");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "RecipientRequired should return error with code and type")]
    public void RecipientRequired_ShouldReturnError()
    {
        Error error = NotificationResult.Failure.RecipientRequired;

        error.Code.Should().Be("Notification.RecipientRequired");
        error.Status.Should().Be(422);
    }
}
