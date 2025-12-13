namespace Bergdahl.Growcube.Client.Events;

public sealed record DeviceVersionEvent(
    string FirmwareVersion,
    long DeviceId,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);