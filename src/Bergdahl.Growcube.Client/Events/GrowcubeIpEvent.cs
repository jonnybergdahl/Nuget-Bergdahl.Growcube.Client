using System.Net;

namespace Bergdahl.Growcube.Client.Events;

public sealed record GrowcubeIpEvent(
    IPAddress Ip,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);