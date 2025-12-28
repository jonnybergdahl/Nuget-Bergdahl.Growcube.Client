namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event indicating the WiFi state of the device.
/// </summary>
/// <param name="StateRaw">The raw WiFi state string.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record WifiStateEvent(
    string StateRaw,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);