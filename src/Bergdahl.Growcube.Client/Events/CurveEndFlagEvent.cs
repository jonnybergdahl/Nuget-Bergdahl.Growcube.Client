namespace Bergdahl.Growcube.Client.Events;

public sealed record CurveEndFlagEvent(
    GrowcubeChannel Channel,
    int Flag,
    int Opcode,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);