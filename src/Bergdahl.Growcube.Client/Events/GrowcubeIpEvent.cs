using System.Net;

namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event containing the IP address of the Growcube device.
/// </summary>
/// <param name="Ip">The IP address of the device.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record GrowcubeIpEvent(
    IPAddress Ip,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);