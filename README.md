# Bergdahl.Growcube.Client

Bergdahl.Growcube.Client

A modern, fully asynchronous .NET client for the Elecrow GrowCube, based on a reverse-engineered TCP protocol.

This library is domain-driven, event-based, and designed for long-running services and automation scenarios.

.NET 8

Async / await throughout

Domain events only (no raw protocol exposure)

Strongly typed commands and events

Curve/history retrieval with normalization helpers

## Installation

```bash
dotnet add package Bergdahl.Growcube.Clientd
```

## Basic usage
### Create and connect a client

```c#
using Bergdahl.Growcube.Client;

var client = new GrowcubeClient(new GrowcubeClientOptions
{
    Host = "192.168.1.42",   // IP of your GrowCube
    AutoSyncTimeOnConnect = true
});

await client.ConnectAsync();
```

> ⚠️ The GrowCube only supports one active TCP client at a time. The official mobile app must not be connected at the same time.

## Listening for events

The GrowCube continuously emits state and telemetry.
You consume this via ListenAsync():

```c#
using Bergdahl.Growcube.Client.Events;

await foreach (var ev in client.ListenAsync())
{
    switch (ev)
    {
        case SensorReadingEvent s:
            Console.WriteLine(
                $"Channel {s.Channel}: {s.MoisturePercent}% moisture, pump open: {s.PumpOpen}");
            break;

        case WaterStateEvent w when w.WaterWarning:
            Console.WriteLine("⚠️ Water tank warning");
            break;

        case DeviceVersionEvent v:
            Console.WriteLine($"Firmware {v.FirmwareVersion}, DeviceId {v.DeviceId}");
            break;
    }
}
```

All events are domain-level. Unknown or future messages are surfaced as UnknownEvent.

## Watering
### Water immediately for a duration

```c#
await client.WaterAsync(
    GrowcubeChannel.A,
    TimeSpan.FromSeconds(5));
```

### Scheduled (manual) watering

```c#
await client.SetScheduledWateringAsync(
    GrowcubeChannel.A,
    duration: TimeSpan.FromSeconds(6),
    interval: TimeSpan.FromHours(48));
```

### Smart watering (moisture-based)

```c#
await client.SetSmartWateringAsync(
    GrowcubeChannel.A,
    minMoisture: 20,
    maxMoisture: 40,
    allowDaylight: true);
```

### Disable watering    

```c#
await client.DisableWateringAsync(GrowcubeChannel.A);
```

### Curve / history data

The GrowCube stores per-day moisture history (“curve data”).

#### Retrieve curve data

```c#
var days = await client.GetCurveDataAsync(
    GrowcubeChannel.A,
    timeout: TimeSpan.FromSeconds(10));
```

Each item represents one day:

```c#
CurveDataEvent
{
    Channel,
    Date,
    Values   // IReadOnlyList<int>
}
```

#### Normalize curve data into a time series

Use the extension helpers in Bergdahl.Growcube.Client.Curve:

```c#
using Bergdahl.Growcube.Client.Curve;

var series = days
    .DeduplicateByDate()
    .ToTimeSeries(TimeSpan.FromMinutes(30));

foreach (var point in series)
{
    Console.WriteLine($"{point.Timestamp}: {point.Value}");
}
```

## Wi-Fi configuration

```c#
await client.SetWifiAsync("MySSID", "MyPassword");
```

⚠️ Expect the connection to drop after sending Wi-Fi settings.

## Discovery

```c#
using Bergdahl.Growcube.Client.Discovery;

var devices = await GrowcubeDiscovery.DiscoverAsync(
    "192.168.1.0/24",
    TimeSpan.FromMilliseconds(500));

foreach (var ip in devices)
    Console.WriteLine($"Found GrowCube at {ip}");
´´´

## Related repositories

 - Protocol reverse-engineering notes:
   https://github.com/jonnybergdahl/Hacking_GrowCube

 - Original Python client:
   https://github.com/jonnybergdahl/Python-growcube-client

## License

MIT, see LICENSE file for details.