namespace Bergdahl.Growcube.Client.Commands;

// Based on observed app behavior; mode 2 is used after SyncTime in your notes. :contentReference[oaicite:7]{index=7}
/// <summary>
/// Represents the work mode for the Growcube.
/// </summary>
public enum GrowcubeWorkMode
{
    /// <summary>
    /// Work mode 0.
    /// </summary>
    Mode0 = 0,

    /// <summary>
    /// Work mode 1.
    /// </summary>
    Mode1 = 1,

    /// <summary>
    /// Work mode 2.
    /// </summary>
    Mode2 = 2
}