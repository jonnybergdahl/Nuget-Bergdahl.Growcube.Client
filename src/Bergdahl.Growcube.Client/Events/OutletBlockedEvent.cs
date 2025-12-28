namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event triggered when an outlet is blocked.
/// </summary>
/// <param name="Channel">The channel that is blocked.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record OutletBlockedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);