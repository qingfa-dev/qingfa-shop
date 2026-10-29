namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ProviderResultTests
{
    [Fact(DisplayName = "Failure.PriorityOutOfRange should include provider name in code and message")]
    public void Failure_PriorityOutOfRange_ShouldIncludeProviderName()
    {
        Error error = ProviderResult.Failure.PriorityOutOfRange("SendGrid");

        error.Code.Should().Be("Provider.SendGrid.Priority.OutOfRange");
        error.Message.Should().Contain("SendGrid");
        error.Message.Should().Contain("Priority");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.RetryCountOutOfRange should include provider name in code and message")]
    public void Failure_RetryCountOutOfRange_ShouldIncludeProviderName()
    {
        Error error = ProviderResult.Failure.RetryCountOutOfRange("Smtp");

        error.Code.Should().Be("Provider.Smtp.RetryCount.OutOfRange");
        error.Message.Should().Contain("Smtp");
        error.Message.Should().Contain("RetryCount");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.TimeoutOutOfRange should include provider name in code and message")]
    public void Failure_TimeoutOutOfRange_ShouldIncludeProviderName()
    {
        Error error = ProviderResult.Failure.TimeoutOutOfRange("Sinch");

        error.Code.Should().Be("Provider.Sinch.Timeout.OutOfRange");
        error.Message.Should().Contain("Sinch");
        error.Message.Should().Contain("Timeout");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Failure.SectionRequired should include provider name in code and message")]
    public void Failure_SectionRequired_ShouldIncludeProviderName()
    {
        Error error = ProviderResult.Failure.SectionRequired("Sinch");

        error.Code.Should().Be("Provider.Sinch.Section.Required");
        error.Message.Should().Contain("Sinch");
        error.Message.Should().Contain("Section");
        error.Status.Should().Be(422);
    }
}
