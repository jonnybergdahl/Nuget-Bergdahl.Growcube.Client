namespace Bergdahl.Growcube.Client.Events;

public sealed record LockStateEvent(
    bool IsLocked,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);