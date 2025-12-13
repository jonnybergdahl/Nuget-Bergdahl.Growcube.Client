namespace Bergdahl.Growcube.Client.Events;

public sealed record SensorReadingEvent(
    GrowcubeChannel Channel,
    int MoisturePercent,
    int HumidityPercent,
    int TemperatureC,
    bool PumpOpen,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);