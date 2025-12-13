namespace Bergdahl.Growcube.Client.Events;

public sealed record OutletBlockedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);