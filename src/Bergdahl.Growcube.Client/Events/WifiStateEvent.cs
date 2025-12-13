namespace Bergdahl.Growcube.Client.Events;

public sealed record WifiStateEvent(
    string StateRaw,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);