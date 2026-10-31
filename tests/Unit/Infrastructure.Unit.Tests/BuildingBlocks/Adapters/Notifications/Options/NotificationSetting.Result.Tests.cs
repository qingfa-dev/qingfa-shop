namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Options;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class NotificationSettingResultTests
{
    [Fact(DisplayName = "Failure.ApplicationNameRequired should return expected error")]
    public void Failure_ApplicationNameRequired_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.ApplicationNameRequired;

        error.Code.Should().Be("Notifications.ApplicationName.Required");
        error.Message.Should().Be("Notifications ApplicationName is required.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidSupportEmail should return expected error")]
    public void Failure_InvalidSupportEmail_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidSupportEmail;

        error.Code.Should().Be("Notifications.SupportEmail.Invalid");
        error.Message.Should().Be("Notifications SupportEmail must be a valid email address.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidSupportPhone should return expected error")]
    public void Failure_InvalidSupportPhone_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidSupportPhone;

        error.Code.Should().Be("Notifications.SupportPhone.Invalid");
        error.Message.Should().Be("Notifications SupportPhone must be a valid phone number.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidApplicationUrl should return expected error")]
    public void Failure_InvalidApplicationUrl_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidApplicationUrl;

        error.Code.Should().Be("Notifications.ApplicationUrl.Invalid");
        error.Message.Should().Be("Notifications ApplicationUrl must be a valid absolute URL.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidCustomerSupportLink should return expected error")]
    public void Failure_InvalidCustomerSupportLink_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidCustomerSupportLink;

        error.Code.Should().Be("Notifications.CustomerSupportLink.Invalid");
        error.Message.Should().Be("Notifications CustomerSupportLink must be a valid absolute URL.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidUnsubscribeUrl should return expected error")]
    public void Failure_InvalidUnsubscribeUrl_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidUnsubscribeUrl;

        error.Code.Should().Be("Notifications.UnsubscribeUrl.Invalid");
        error.Message.Should().Be("Notifications UnsubscribeUrl must be a valid absolute URL.");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.InvalidSurveyUrl should return expected error")]
    public void Failure_InvalidSurveyUrl_ShouldReturnExpectedError()
    {
        Error error = NotificationSettingOutcome.Failure.InvalidSurveyUrl;

        error.Code.Should().Be("Notifications.SurveyUrl.Invalid");
        error.Message.Should().Be("Notifications SurveyUrl must be a valid absolute URL.");
        error.Status.Should().Be(422);
    }
}
