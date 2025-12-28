namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event indicating the water state (warning) of the device.
/// </summary>
/// <param name="WaterWarning">True if there is a water warning (e.g., low water), otherwise false.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record WaterStateEvent(
    bool WaterWarning,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);