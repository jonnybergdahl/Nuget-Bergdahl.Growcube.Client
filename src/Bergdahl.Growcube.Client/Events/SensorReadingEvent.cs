namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event containing sensor readings for a channel.
/// </summary>
/// <param name="Channel">The channel.</param>
/// <param name="MoisturePercent">The moisture percentage.</param>
/// <param name="HumidityPercent">The humidity percentage.</param>
/// <param name="TemperatureC">The temperature in degrees Celsius.</param>
/// <param name="PumpOpen">True if the pump is open, otherwise false.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record SensorReadingEvent(
    GrowcubeChannel Channel,
    int MoisturePercent,
    int HumidityPercent,
    int TemperatureC,
    bool PumpOpen,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);