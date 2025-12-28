namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event indicating the end of a curve data transmission.
/// </summary>
/// <param name="Channel">The channel.</param>
/// <param name="Flag">The end flag value.</param>
/// <param name="Opcode">The opcode associated with the end flag.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record CurveEndFlagEvent(
    GrowcubeChannel Channel,
    int Flag,
    int Opcode,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);