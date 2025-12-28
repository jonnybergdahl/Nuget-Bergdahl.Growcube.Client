namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event indicating the lock state of the device.
/// </summary>
/// <param name="IsLocked">True if the device is locked, otherwise false.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record LockStateEvent(
    bool IsLocked,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);