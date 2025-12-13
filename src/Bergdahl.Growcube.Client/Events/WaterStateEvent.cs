namespace Bergdahl.Growcube.Client.Events;

public sealed record WaterStateEvent(
    bool WaterWarning,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);