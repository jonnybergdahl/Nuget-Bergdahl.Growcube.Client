using Bergdahl.Growcube.Client.Events;
using Bergdahl.Growcube.Client.Protocol;

namespace Bergdahl.Growcube.Client.Internal;

internal static class EventMapper
{
    public static GrowcubeEvent Map(GrowcubeFrame frame, DateTimeOffset ts)
        => frame.Opcode switch
        {
            24 => ParseDeviceVersion(frame.Payload, ts),
            21 => ParseSensorReading(frame.Payload, ts),
            20 => ParseWaterState(frame.Payload, ts),
            33 => ParseLockState(frame.Payload, ts),

            26 => ParsePumpState(frame.Payload, isOpen: true, ts),
            27 => ParsePumpState(frame.Payload, isOpen: false, ts),
            28 => ParseSensorCheck(frame.Payload, ts),
            29 => ParseOutletBlocked(frame.Payload, ts),
            30 => ParseSensorNotConnected(frame.Payload, ts),
            31 => new WifiStateEvent(frame.Payload, ts),
            32 => ParseIp(frame.Payload, ts),
            34 => ParseOutletLocked(frame.Payload, ts),

            22 => ParseCurveData(frame.Payload, ts),
            23 => ParseAutoWaterTimestamp(frame.Payload, ts),
            35 or 36 => ParseCurveEndFlag(frame.Opcode, frame.Payload, ts),

            _ => new UnknownEvent(frame.Opcode, frame.Payload, ts)
        };

    private static GrowcubeEvent ParseDeviceVersion(string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');
        var version = parts.Length > 0 ? parts[0] : string.Empty;

        long deviceId = 0;
        if (parts.Length > 1)
            _ = long.TryParse(parts[1], out deviceId);

        return new DeviceVersionEvent(version, deviceId, ts);
    }

    private static GrowcubeEvent ParseSensorReading(string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');

        int chRaw = 0, moisture = 0, humidity = 0, temp = 0;
        if (parts.Length > 0) _ = int.TryParse(parts[0], out chRaw);
        if (parts.Length > 1) _ = int.TryParse(parts[1], out moisture);
        if (parts.Length > 2) _ = int.TryParse(parts[2], out humidity);
        if (parts.Length > 3) _ = int.TryParse(parts[3], out temp);

        var channel = Enum.IsDefined(typeof(GrowcubeChannel), chRaw)
            ? (GrowcubeChannel)chRaw
            : GrowcubeChannel.A;

        return new SensorReadingEvent(channel, moisture, humidity, temp, PumpOpen: false, ts);
    }

    private static GrowcubeEvent ParseWaterState(string payload, DateTimeOffset ts)
    {
        var waterEnough = payload.Trim() == "1";
        return new WaterStateEvent(WaterWarning: !waterEnough, ts);
    }

    private static GrowcubeEvent ParseLockState(string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');

        int a = 0, b = 0;
        if (parts.Length > 0) _ = int.TryParse(parts[0], out a);
        if (parts.Length > 1) _ = int.TryParse(parts[1], out b);

        return new LockStateEvent(IsLocked: a != 0 || b != 0, ts);
    }

    private static GrowcubeChannel ParseChannel(string payload)
    {
        var trimmed = payload.Trim();
        if (int.TryParse(trimmed, out var chRaw) && Enum.IsDefined(typeof(GrowcubeChannel), chRaw))
            return (GrowcubeChannel)chRaw;

        return GrowcubeChannel.A;
    }

    private static GrowcubeEvent ParsePumpState(string payload, bool isOpen, DateTimeOffset ts)
        => new PumpStateEvent(ParseChannel(payload), isOpen, ts);

    private static GrowcubeEvent ParseSensorCheck(string payload, DateTimeOffset ts)
        => new SensorCheckEvent(ParseChannel(payload), ts);

    private static GrowcubeEvent ParseOutletBlocked(string payload, DateTimeOffset ts)
        => new OutletBlockedEvent(ParseChannel(payload), ts);

    private static GrowcubeEvent ParseSensorNotConnected(string payload, DateTimeOffset ts)
        => new SensorNotConnectedEvent(ParseChannel(payload), ts);

    private static GrowcubeEvent ParseOutletLocked(string payload, DateTimeOffset ts)
        => new OutletLockedEvent(ParseChannel(payload), ts);

    private static GrowcubeEvent ParseIp(string payload, DateTimeOffset ts)
    {
        var trimmed = payload.Trim();
        return System.Net.IPAddress.TryParse(trimmed, out var ip)
            ? new GrowcubeIpEvent(ip, ts)
            : new UnknownEvent(32, payload, ts);
    }

    private static GrowcubeEvent ParseCurveData(string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');
        if (parts.Length < 5) return new UnknownEvent(22, payload, ts);

        if (!int.TryParse(parts[0], out var chRaw)) return new UnknownEvent(22, payload, ts);
        if (!int.TryParse(parts[1], out var year)) return new UnknownEvent(22, payload, ts);
        if (!int.TryParse(parts[2], out var month)) return new UnknownEvent(22, payload, ts);
        if (!int.TryParse(parts[3], out var day)) return new UnknownEvent(22, payload, ts);

        var channel = Enum.IsDefined(typeof(GrowcubeChannel), chRaw) ? (GrowcubeChannel)chRaw : GrowcubeChannel.A;

        DateOnly date;
        try { date = new DateOnly(year, month, day); }
        catch { return new UnknownEvent(22, payload, ts); }

        var rawValues = parts[4].Split(',');
        var values = new List<int>(rawValues.Length);
        for (var i = 0; i < rawValues.Length; i++)
        {
            var s = rawValues[i].Trim();
            if (s.Length == 0) { values.Add(0); continue; }
            if (!int.TryParse(s, out var v)) return new UnknownEvent(22, payload, ts);
            values.Add(v);
        }

        return new CurveDataEvent(channel, date, values, ts);
    }

    private static GrowcubeEvent ParseAutoWaterTimestamp(string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');
        if (parts.Length < 6) return new UnknownEvent(23, payload, ts);

        if (!int.TryParse(parts[0], out var chRaw)) return new UnknownEvent(23, payload, ts);
        if (!int.TryParse(parts[1], out var year)) return new UnknownEvent(23, payload, ts);
        if (!int.TryParse(parts[2], out var month)) return new UnknownEvent(23, payload, ts);
        if (!int.TryParse(parts[3], out var day)) return new UnknownEvent(23, payload, ts);
        if (!int.TryParse(parts[4], out var hour)) return new UnknownEvent(23, payload, ts);
        if (!int.TryParse(parts[5], out var minute)) return new UnknownEvent(23, payload, ts);

        var channel = Enum.IsDefined(typeof(GrowcubeChannel), chRaw) ? (GrowcubeChannel)chRaw : GrowcubeChannel.A;

        DateTime local;
        try { local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified); }
        catch { return new UnknownEvent(23, payload, ts); }

        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new AutoWaterTimestampEvent(channel, new DateTimeOffset(local, offset), ts);
    }

    private static GrowcubeEvent ParseCurveEndFlag(int opcode, string payload, DateTimeOffset ts)
    {
        var parts = payload.Split('@');
        if (parts.Length < 2) return new UnknownEvent(opcode, payload, ts);

        if (!int.TryParse(parts[0], out var chRaw)) return new UnknownEvent(opcode, payload, ts);
        if (!int.TryParse(parts[1], out var flag)) return new UnknownEvent(opcode, payload, ts);

        var channel = Enum.IsDefined(typeof(GrowcubeChannel), chRaw) ? (GrowcubeChannel)chRaw : GrowcubeChannel.A;
        return new CurveEndFlagEvent(channel, flag, opcode, ts);
    }
}
