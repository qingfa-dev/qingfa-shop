namespace Infrastructure.Unit.Tests.Integrations.Notifications.Options.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class ProviderExtensionsTests
{
    private sealed class TestProvider : IProviderSetting
    {
        public static string Section => "TestProvider";
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public int RetryCount { get; set; } = 3;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    [Fact(DisplayName = "ValidateBase should return no errors for valid provider")]
    public void ValidateBase_ValidProvider_ShouldReturnNoErrors()
    {
        TestProvider provider = new()
        {
            Priority = 5,
            RetryCount = 2,
            Timeout = TimeSpan.FromSeconds(60),
        };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "ValidateBase should fail when Priority is below min")]
    public void ValidateBase_PriorityBelowMin_ShouldFail()
    {
        TestProvider provider = new() { Priority = 0 };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.Priority.OutOfRange");
    }

    [Fact(DisplayName = "ValidateBase should fail when Priority is above max")]
    public void ValidateBase_PriorityAboveMax_ShouldFail()
    {
        TestProvider provider = new() { Priority = 101 };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.Priority.OutOfRange");
    }

    [Fact(DisplayName = "ValidateBase should fail when RetryCount is below min")]
    public void ValidateBase_RetryCountBelowMin_ShouldFail()
    {
        TestProvider provider = new() { RetryCount = -1 };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.RetryCount.OutOfRange");
    }

    [Fact(DisplayName = "ValidateBase should fail when RetryCount is above max")]
    public void ValidateBase_RetryCountAboveMax_ShouldFail()
    {
        TestProvider provider = new() { RetryCount = 11 };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.RetryCount.OutOfRange");
    }

    [Fact(DisplayName = "ValidateBase should fail when Timeout is below min")]
    public void ValidateBase_TimeoutBelowMin_ShouldFail()
    {
        TestProvider provider = new() { Timeout = TimeSpan.FromSeconds(0) };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.Timeout.OutOfRange");
    }

    [Fact(DisplayName = "ValidateBase should fail when Timeout is above max")]
    public void ValidateBase_TimeoutAboveMax_ShouldFail()
    {
        TestProvider provider = new() { Timeout = TimeSpan.FromSeconds(301) };

        List<Error> errors = provider.ValidateBase().ToList();

        errors.Should().ContainSingle(e => e.Code == "Provider.TestProvider.Timeout.OutOfRange");
    }

    [Fact(DisplayName = "ProviderResult.SectionNameRequired should format correctly")]
    public void ProviderResult_SectionNameRequired_ShouldFormatCorrectly()
    {
        Error error = ProviderResult.Failure.SectionRequired("EmptyProvider");

        error.Code.Should().Be("Provider.EmptyProvider.Section.Required");
    }
}
