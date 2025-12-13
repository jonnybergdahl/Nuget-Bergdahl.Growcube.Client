using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Bergdahl.Growcube.Client.Commands;
using Bergdahl.Growcube.Client.Events;
using Bergdahl.Growcube.Client.Protocol;

namespace Bergdahl.Growcube.Client;

public interface IGrowcubeClient : IAsyncDisposable
{
    bool IsConnected { get; }

    ValueTask ConnectAsync(CancellationToken ct = default);
    ValueTask DisconnectAsync(CancellationToken ct = default);

    IAsyncEnumerable<GrowcubeEvent> ListenAsync(CancellationToken ct = default);

    ValueTask SyncTimeAsync(DateTimeOffset? now = null, CancellationToken ct = default);
    ValueTask SetWorkModeAsync(GrowcubeWorkMode mode, CancellationToken ct = default);

    ValueTask WaterAsync(GrowcubeChannel channel, TimeSpan duration, CancellationToken ct = default);

    ValueTask SetSmartWateringAsync(
        GrowcubeChannel channel,
        int minMoisture,
        int maxMoisture,
        bool allowDaylight,
        CancellationToken ct = default);

    ValueTask SetScheduledWateringAsync(
        GrowcubeChannel channel,
        TimeSpan duration,
        TimeSpan interval,
        CancellationToken ct = default);

    ValueTask DisableWateringAsync(GrowcubeChannel channel, CancellationToken ct = default);

    ValueTask SetWifiAsync(string ssid, string password, CancellationToken ct = default);
    
    ValueTask<IReadOnlyList<CurveDataEvent>> GetCurveDataAsync(
        GrowcubeChannel channel,
        TimeSpan timeout,
        CancellationToken ct = default);    
}

public sealed class GrowcubeClient : IGrowcubeClient
{
    private readonly Channel<GrowcubeEvent> _events =
        Channel.CreateUnbounded<GrowcubeEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

    private readonly GrowcubeClientOptions _options;

    private readonly GrowcubeFrameParser _parser = new();

    private CancellationTokenSource? _loopCts;
    private Task? _readLoop;
    private NetworkStream? _stream;

    private TcpClient? _tcp;

    private readonly object _curveLock = new();
    private readonly Dictionary<GrowcubeChannel, CurveCollector> _curveCollectors = new();
    
    private readonly bool[] _pumpOpenByChannel = new bool[4];
    
    public GrowcubeClient(GrowcubeClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsConnected => _tcp is not null && _tcp.Connected;

    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        if (_tcp is not null)
            throw new InvalidOperationException("Client is already connected (or connecting).");

        var tcp = new TcpClient();
        await tcp.ConnectAsync(_options.Host, _options.Port, ct).ConfigureAwait(false);

        var stream = tcp.GetStream();
        stream.ReadTimeout = (int)_options.ReadTimeout.TotalMilliseconds;

        _tcp = tcp;
        _stream = stream;

        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _readLoop = Task.Run(() => ReadLoopAsync(_loopCts.Token), CancellationToken.None);

        if (_options.AutoSyncTimeOnConnect)
            await SyncTimeAsync(DateTimeOffset.Now, ct).ConfigureAwait(false);
    }

    public async ValueTask DisconnectAsync(CancellationToken ct = default)
    {
        _loopCts?.Cancel();

        var loop = _readLoop;
        if (loop is not null)
            try
            {
                await loop.WaitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                /* ignore */
            }

        try
        {
            _stream?.Dispose();
        }
        catch
        {
            /* ignore */
        }

        try
        {
            _tcp?.Dispose();
        }
        catch
        {
            /* ignore */
        }

        _stream = null;
        _tcp = null;

        _loopCts?.Dispose();
        _loopCts = null;
        _readLoop = null;
    }

    public async ValueTask SyncTimeAsync(DateTimeOffset? now = null, CancellationToken ct = default)
    {
        await SendAsync(InternalCommands.SyncTime(now ?? DateTimeOffset.Now), ct).ConfigureAwait(false);
    }

    public async ValueTask SetWorkModeAsync(GrowcubeWorkMode mode, CancellationToken ct = default)
    {
        await SendAsync(InternalCommands.SetWorkMode(mode), ct).ConfigureAwait(false);
    }

    public async ValueTask WaterAsync(GrowcubeChannel channel, TimeSpan duration, CancellationToken ct = default)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        await SendAsync(InternalCommands.Water(channel, true), ct).ConfigureAwait(false);

        try
        {
            await Task.Delay(duration, ct).ConfigureAwait(false);
        }
        finally
        {
            // Ensure pump stop even on cancellation
            await SendAsync(InternalCommands.Water(channel, false), CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    public async ValueTask SetSmartWateringAsync(
        GrowcubeChannel channel,
        int minMoisture,
        int maxMoisture,
        bool allowDaylight,
        CancellationToken ct = default)
    {
        if ((uint)minMoisture > 100u) throw new ArgumentOutOfRangeException(nameof(minMoisture));
        if ((uint)maxMoisture > 100u) throw new ArgumentOutOfRangeException(nameof(maxMoisture));
        if (minMoisture >= maxMoisture) throw new ArgumentException("minMoisture must be < maxMoisture.");

        var mode = allowDaylight
            ? InternalCommands.WateringMode.SmartOutsideDaylight
            : InternalCommands.WateringMode.Smart;

        await SendAsync(
                InternalCommands.SetWaterMode(channel, mode, minMoisture.ToString(), maxMoisture.ToString()),
                ct)
            .ConfigureAwait(false);
    }

    public async ValueTask SetScheduledWateringAsync(
        GrowcubeChannel channel,
        TimeSpan duration,
        TimeSpan interval,
        CancellationToken ct = default)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        var durationSeconds = (int)Math.Round(duration.TotalSeconds);
        var intervalHours = (int)Math.Round(interval.TotalHours);

        if (durationSeconds <= 0) durationSeconds = 1;
        if (intervalHours <= 0) intervalHours = 1;

        await SendAsync(
                InternalCommands.SetWaterMode(
                    channel,
                    InternalCommands.WateringMode.Manual,
                    $"{durationSeconds}s",
                    intervalHours.ToString()),
                ct)
            .ConfigureAwait(false);
    }

    public async ValueTask DisableWateringAsync(GrowcubeChannel channel, CancellationToken ct = default)
    {
        await SendAsync(InternalCommands.ClosePump(channel), ct).ConfigureAwait(false);
    }

    public async ValueTask SetWifiAsync(string ssid, string password, CancellationToken ct = default)
    {
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await SendAsync(InternalCommands.WifiSettings(ssid, password, ms), ct).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<GrowcubeEvent> ListenAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (await _events.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
        while (_events.Reader.TryRead(out var ev))
            yield return ev;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _events.Writer.TryComplete();
    }

    private async ValueTask SendAsync(byte[] data, CancellationToken ct)
    {
        var stream = _stream;
        if (stream is null)
            throw new InvalidOperationException("Not connected.");

        await stream.WriteAsync(data, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        Exception? error = null;

        try
        {
            var buffer = new byte[8192];

            while (!ct.IsCancellationRequested)
            {
                var stream = _stream;
                if (stream is null) break;

                var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
                if (read == 0) break;

                _parser.Append(buffer.AsSpan(0, read));

                foreach (var frame in _parser.Drain())
                {
                    var ev = Internal.EventMapper.Map(frame, DateTimeOffset.Now);

                    // Update internal pump state + enrich sensor readings
                    ev = ApplyPumpState(ev);
                    
                    TryHandleCurveCollection(ev);

                    _events.Writer.TryWrite(ev);
                }
            }
        }
        catch (OperationCanceledException)
        {
            /* normal */
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _events.Writer.TryComplete(error);
        }
    }
    
    public async ValueTask<IReadOnlyList<CurveDataEvent>> GetCurveDataAsync(
        GrowcubeChannel channel,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");

        CurveCollector collector;

        lock (_curveLock)
        {
            if (_curveCollectors.ContainsKey(channel))
                throw new InvalidOperationException($"A curve request is already in progress for channel {channel}.");

            collector = new CurveCollector();
            _curveCollectors[channel] = collector;
        }

        try
        {
            // Send request (opcode 48 in your notes: ReqCurveDataCmd). 
            await SendAsync(InternalCommands.RequestCurveData(channel), ct).ConfigureAwait(false);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await collector.Completion.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout (not external cancellation)
                throw new TimeoutException($"Timed out waiting for curve data for channel {channel} after {timeout}.");
            }
        }
        finally
        {
            lock (_curveLock)
            {
                _curveCollectors.Remove(channel);
            }
        }
    }
    
    private void TryHandleCurveCollection(GrowcubeEvent ev)
    {
        switch (ev)
        {
            case CurveDataEvent curve:
                lock (_curveLock)
                {
                    if (_curveCollectors.TryGetValue(curve.Channel, out var collector))
                        collector.Items.Add(curve);
                }
                break;

            case CurveEndFlagEvent end:
                lock (_curveLock)
                {
                    if (_curveCollectors.TryGetValue(end.Channel, out var collector))
                        collector.Completion.TrySetResult(collector.Items.AsReadOnly());
                }
                break;

            default:
                break;
        }
        
    }

    private GrowcubeEvent ApplyPumpState(GrowcubeEvent ev)
    {
        // Pump open/close events update the state cache.
        if (ev is PumpStateEvent ps)
        {
            var idx = (int)ps.Channel;
            if ((uint)idx < (uint)_pumpOpenByChannel.Length)
                _pumpOpenByChannel[idx] = ps.IsOpen;

            return ev;
        }

        // Sensor readings get enriched with latest pump state for that channel.
        if (ev is SensorReadingEvent sr)
        {
            var idx = (int)sr.Channel;
            if ((uint)idx < (uint)_pumpOpenByChannel.Length)
            {
                var isOpen = _pumpOpenByChannel[idx];
                if (sr.PumpOpen != isOpen)
                    return sr with { PumpOpen = isOpen };
            }

            return ev;
        }

        return ev;
    }

    private sealed class CurveCollector
    {
        public CurveCollector()
        {
            Completion = new TaskCompletionSource<IReadOnlyList<CurveDataEvent>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public List<CurveDataEvent> Items { get; } = new();
        public TaskCompletionSource<IReadOnlyList<CurveDataEvent>> Completion { get; }
    }    
}