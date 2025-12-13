namespace Bergdahl.Growcube.Client.Events;

public sealed record OutletLockedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);