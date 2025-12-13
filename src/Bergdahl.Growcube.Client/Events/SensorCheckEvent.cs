namespace Bergdahl.Growcube.Client.Events;

/// <summary>
///     Reported during periodic status (opcode 28 in your notes).
/// </summary>
public sealed record SensorCheckEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);