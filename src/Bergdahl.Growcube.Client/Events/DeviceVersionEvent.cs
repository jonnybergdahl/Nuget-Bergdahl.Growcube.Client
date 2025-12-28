namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event containing device version information.
/// </summary>
/// <param name="FirmwareVersion">The firmware version of the device.</param>
/// <param name="DeviceId">The unique identifier of the device.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record DeviceVersionEvent(
    string FirmwareVersion,
    long DeviceId,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);