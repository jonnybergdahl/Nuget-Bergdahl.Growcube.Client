namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event triggered during periodic status checks (opcode 28).
/// </summary>
/// <param name="Channel">The channel being checked.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record SensorCheckEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);