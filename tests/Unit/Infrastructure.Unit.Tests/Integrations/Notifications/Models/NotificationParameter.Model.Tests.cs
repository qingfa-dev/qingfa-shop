namespace Infrastructure.Unit.Tests.Integrations.Notifications.Models;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationParameterModelTests
{
    public static IEnumerable<object?[]> CreateData()
    {
        yield return [NotificationParameterType.ApplicationName, "ReSys Shop", true];
        yield return [NotificationParameterType.VerificationCode, "123456", true];
        yield return [NotificationParameterType.OrderTotal, "$49.99", true];
        yield return [NotificationParameterType.UserFirstName, "Alice", true];
    }

    [Theory(DisplayName = "Create should return parameter with matching values")]
    [MemberData(nameof(CreateData))]
    public void Create_ShouldReturnMatchingParameter(NotificationParameterType key, string? value, bool isRequired)
    {
        NotificationParameter parameter = NotificationParameter.Create(key, value, isRequired);

        parameter.Key.Should().Be(key);
        parameter.Value.Should().Be(value);
        parameter.IsRequired.Should().Be(isRequired);
    }

    [Fact(DisplayName = "Create with default isRequired should be true")]
    public void Create_WithDefaultIsRequired_ShouldBeTrue()
    {
        NotificationParameter parameter = NotificationParameter.Create(NotificationParameterType.ApplicationName, "ReSys");

        parameter.IsRequired.Should().BeTrue();
    }

    [Fact(DisplayName = "Equal instances should be equal")]
    public void EqualInstances_ShouldBeEqual()
    {
        NotificationParameter a = NotificationParameter.Create(NotificationParameterType.SupportEmail, "a@b.com");
        NotificationParameter b = NotificationParameter.Create(NotificationParameterType.SupportEmail, "a@b.com");

        a.Should().Be(b);
    }
}
