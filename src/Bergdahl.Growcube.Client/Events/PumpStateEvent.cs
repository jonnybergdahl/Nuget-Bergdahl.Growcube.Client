namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event indicating the state of a pump.
/// </summary>
/// <param name="Channel">The channel associated with the pump.</param>
/// <param name="IsOpen">True if the pump is open (running), otherwise false.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record PumpStateEvent(
    GrowcubeChannel Channel,
    bool IsOpen,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);