namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event triggered when a sensor is not connected.
/// </summary>
/// <param name="Channel">The channel associated with the missing sensor.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record SensorNotConnectedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);