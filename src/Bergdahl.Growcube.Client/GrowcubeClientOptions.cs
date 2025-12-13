namespace Bergdahl.Growcube.Client;

public sealed record GrowcubeClientOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 8800;
    public bool AutoSyncTimeOnConnect { get; init; } = true;
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);
}