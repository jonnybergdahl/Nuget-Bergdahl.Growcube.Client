namespace Bergdahl.Growcube.Client.Events;

public sealed record SensorNotConnectedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);