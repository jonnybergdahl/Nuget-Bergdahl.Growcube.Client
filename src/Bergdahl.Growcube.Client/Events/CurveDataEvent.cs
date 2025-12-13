namespace Bergdahl.Growcube.Client.Events;

public sealed record CurveDataEvent(
    GrowcubeChannel Channel,
    DateOnly Date,
    IReadOnlyList<int> Values,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);