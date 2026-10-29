namespace Infrastructure.Unit.Tests.Integrations.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationContextModelTests
{
    [Fact(DisplayName = "Empty should have no parameters")]
    public void Empty_ShouldHaveNoParameters()
    {
        NotificationContext context = NotificationContext.Empty;

        context.Parameters.Should().BeEmpty();
    }

    [Fact(DisplayName = "Create with items should return context with matching parameters")]
    public void Create_WithItems_ShouldReturnContextWithMatchingParameters()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "ReSys"),
            (NotificationParameterType.VerificationCode, "123456"));

        context.Parameters.Should().HaveCount(2);
        context.Parameters.Should().Contain(p => p.Key == NotificationParameterType.ApplicationName && p.Value == "ReSys");
        context.Parameters.Should().Contain(p => p.Key == NotificationParameterType.VerificationCode && p.Value == "123456");
    }

    [Fact(DisplayName = "Create with duplicate keys should use last-one-wins")]
    public void Create_WithDuplicateKeys_ShouldUseLastOneWins()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "First"),
            (NotificationParameterType.ApplicationName, "Last"));

        context.Parameters.Should().HaveCount(1);
        context.Parameters.Should().ContainSingle(p => p.Key == NotificationParameterType.ApplicationName);
        context.Parameters.Should().Contain(p => p.Key == NotificationParameterType.ApplicationName && p.Value == "Last");
    }

    [Fact(DisplayName = "GetValue should return value for existing key")]
    public void GetValue_ForExistingKey_ShouldReturnValue()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.UserFirstName, "Alice"));

        context.GetValue(NotificationParameterType.UserFirstName).Should().Be("Alice");
    }

    [Fact(DisplayName = "GetValue should return null for missing key")]
    public void GetValue_ForMissingKey_ShouldReturnNull()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "ReSys"));

        context.GetValue(NotificationParameterType.UserFirstName).Should().BeNull();
    }

    [Fact(DisplayName = "GetValue should return last value for duplicate keys")]
    public void GetValue_ForDuplicateKeys_ShouldReturnLastValue()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.VerificationCode, "111111"),
            (NotificationParameterType.VerificationCode, "222222"),
            (NotificationParameterType.VerificationCode, "333333"));

        context.GetValue(NotificationParameterType.VerificationCode).Should().Be("333333");
    }

    [Fact(DisplayName = "ApplyParameter should add new parameter")]
    public void ApplyParameter_ShouldAddNewParameter()
    {
        NotificationContext context = NotificationContext.Empty;

        NotificationContext result = NotificationContext.ApplyParameter(context, NotificationParameterType.ApplicationName, "ReSys");

        result.Parameters.Should().ContainSingle(p => p.Key == NotificationParameterType.ApplicationName && p.Value == "ReSys");
    }

    [Fact(DisplayName = "ApplyParameter should replace existing parameter")]
    public void ApplyParameter_ShouldReplaceExistingParameter()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.SupportEmail, "old@test.com"));

        NotificationContext result = NotificationContext.ApplyParameter(context, NotificationParameterType.SupportEmail, "new@test.com");

        result.Parameters.Should().ContainSingle(p => p.Key == NotificationParameterType.SupportEmail);
        result.Parameters.Should().Contain(p => p.Key == NotificationParameterType.SupportEmail && p.Value == "new@test.com");
    }

    [Fact(DisplayName = "ApplyParameter should not mutate original context")]
    public void ApplyParameter_ShouldNotMutateOriginalContext()
    {
        NotificationContext context = NotificationContext.Create(
            (NotificationParameterType.ApplicationName, "Original"));

        NotificationContext result = NotificationContext.ApplyParameter(context, NotificationParameterType.SupportEmail, "test@test.com");

        result.GetValue(NotificationParameterType.ApplicationName).Should().Be("Original");
        context.GetValue(NotificationParameterType.SupportEmail).Should().BeNull();
    }
}
