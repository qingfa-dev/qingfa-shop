namespace Application.BuildingBlocks.Messaging.Serialization;

public interface IEventTypeRegistry
{
    Type Resolve(string eventName, int eventVersion);
}
