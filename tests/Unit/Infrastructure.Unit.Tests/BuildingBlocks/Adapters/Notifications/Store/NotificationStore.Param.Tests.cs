using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications.Store;
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public sealed class NotificationStoreParamTests
{
    [Fact(DisplayName = "Parameters should contain param values for each template")]
    public void Parameters_ShouldContainParamValues()
    {
        foreach (KeyValuePair<NotificationUseCase, NotificationTemplate> entry in NotificationStore.Templates)
        {
            entry.Value.ParamValues.Should().NotBeNull();
        }
    }
    [Fact(DisplayName = "Parameters store should contain all defined NotificationParameterType values")]
    public void Parameters_ShouldContainExpectedKeys()
    {
        NotificationParameterType[] paramTypes = Enum.GetValues<NotificationParameterType>();
        foreach (NotificationParameterType paramType in paramTypes)
        {
            NotificationStore.Parameters.Should().ContainKey(paramType);
        }
    }
}
