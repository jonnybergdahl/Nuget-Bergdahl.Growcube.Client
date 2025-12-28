namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event triggered when an automatic watering timestamp is received.
/// </summary>
/// <param name="Channel">The channel that was watered.</param>
/// <param name="When">The timestamp of the watering.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record AutoWaterTimestampEvent(
    GrowcubeChannel Channel,
    DateTimeOffset When,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);