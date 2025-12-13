namespace Bergdahl.Growcube.Client.Events;

public sealed record AutoWaterTimestampEvent(
    GrowcubeChannel Channel,
    DateTimeOffset When,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);