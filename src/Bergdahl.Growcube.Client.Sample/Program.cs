using Bergdahl.Growcube.Client;
using Bergdahl.Growcube.Client.Commands;
using Bergdahl.Growcube.Client.Curve;
using Bergdahl.Growcube.Client.Events;

static string? GetArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : null;
}

static bool HasFlag(string[] args, string name)
    => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

var host = GetArg(args, "--host") ?? GetArg(args, "-h");
if (string.IsNullOrWhiteSpace(host))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("--host <ip> [--workmode 2] [--curve A] [--auto-water]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("--host 192.168.1.42");
    Console.WriteLine("--host 192.168.1.42 --workmode 2");
    Console.WriteLine("--host 192.168.1.42 --curve A");
    Console.WriteLine("--host 192.168.1.42 --auto-water");
    return;
}

var workModeArg = GetArg(args, "--workmode");
var autoWater = HasFlag(args, "--auto-water");
var curveArg = GetArg(args, "--curve"); // A/B/C/D

var options = new GrowcubeClientOptions
{
    Host = host,
    AutoSyncTimeOnConnect = true
};

await using var client = new GrowcubeClient(options);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Connecting to GrowCube at {host}...");
await client.ConnectAsync(cts.Token);
Console.WriteLine("Connected.");

if (!string.IsNullOrWhiteSpace(workModeArg) && int.TryParse(workModeArg, out var wmInt))
{
    var mode = wmInt switch
    {
        0 => GrowcubeWorkMode.Mode0,
        1 => GrowcubeWorkMode.Mode1,
        _ => GrowcubeWorkMode.Mode2
    };

    Console.WriteLine($"Setting work mode to {mode}...");
    await client.SetWorkModeAsync(mode, cts.Token);
}

if (!string.IsNullOrWhiteSpace(curveArg))
{
    var channel = curveArg.Trim().ToUpperInvariant() switch
    {
        "A" => GrowcubeChannel.A,
        "B" => GrowcubeChannel.B,
        "C" => GrowcubeChannel.C,
        "D" => GrowcubeChannel.D,
        _ => GrowcubeChannel.A
    };

    Console.WriteLine($"Requesting curve data for channel {channel}...");
    var days = await client.GetCurveDataAsync(channel, timeout: TimeSpan.FromSeconds(10), cts.Token);

    // Choose an assumed sample interval. Adjust if you know the device’s actual cadence.
    var series = days.DeduplicateByDate().ToTimeSeries(TimeSpan.FromMinutes(30));

    Console.WriteLine($"Curve packets: {days.Count}, normalized points: {series.Count}");
    foreach (var p in series.Take(20))
        Console.WriteLine($"{p.Timestamp:yyyy-MM-dd HH:mm:ss zzz}  {p.Value}");

    Console.WriteLine("Curve request completed.");
}

Console.WriteLine("Listening for events. Press Ctrl+C to exit.");

await foreach (var ev in client.ListenAsync(cts.Token))
{
    // Similar “print everything” behavior as the Python sample
    Console.WriteLine($"{ev.Timestamp:O}  {ev}");

    // Optional demo behavior: if moisture below threshold, water for 5 seconds
    if (autoWater && ev is SensorReadingEvent s)
    {
        if (s.MoisturePercent < 20)
        {
            Console.WriteLine($"Moisture low on {s.Channel} ({s.MoisturePercent}%). Watering for 5s...");
            try
            {
                await client.WaterAsync(s.Channel, TimeSpan.FromSeconds(5), cts.Token);
                Console.WriteLine("Watering completed.");
            }
            catch (OperationCanceledException)
            {
                // exiting
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Watering failed: {ex.Message}");
            }
        }
    }
}

Console.WriteLine("Disconnected.");
