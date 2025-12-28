namespace Bergdahl.Growcube.Client;

/// <summary>
/// Configuration options for the Growcube client.
/// </summary>
public sealed record GrowcubeClientOptions
{
    /// <summary>
    /// The host name or IP address of the Growcube device.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// The port number of the Growcube device. Defaults to 8800.
    /// </summary>
    public int Port { get; init; } = 8800;

    /// <summary>
    /// Whether to automatically synchronize the time on connection. Defaults to true.
    /// </summary>
    public bool AutoSyncTimeOnConnect { get; init; } = true;

    /// <summary>
    /// The read timeout for network operations. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);
}