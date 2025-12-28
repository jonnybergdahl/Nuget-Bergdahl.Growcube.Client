namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Base class for all Growcube events.
/// </summary>
/// <param name="Timestamp">The timestamp of the event.</param>
public abstract record GrowcubeEvent(DateTimeOffset Timestamp);