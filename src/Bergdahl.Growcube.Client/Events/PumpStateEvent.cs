namespace Bergdahl.Growcube.Client.Events;

public sealed record PumpStateEvent(
    GrowcubeChannel Channel,
    bool IsOpen,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);