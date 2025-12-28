namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event triggered when an outlet is locked.
/// </summary>
/// <param name="Channel">The channel that is locked.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record OutletLockedEvent(
    GrowcubeChannel Channel,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);