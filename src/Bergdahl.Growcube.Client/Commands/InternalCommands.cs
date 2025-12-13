using Bergdahl.Growcube.Client.Protocol;

namespace Bergdahl.Growcube.Client.Commands;

internal static class InternalCommands
{
    public enum WateringMode
    {
        Manual = 1,
        SmartOutsideDaylight = 2,
        Smart = 3
    }

    public static byte[] SyncTime(DateTimeOffset now)
    {
        // yyyy@MM@dd@HH@mm@ss in your notes (example: elea44#...#2023@07@12@22@49@05#) :contentReference[oaicite:8]{index=8}
        var payload = $"{now:yyyy}@{now:MM}@{now:dd}@{now:HH}@{now:mm}@{now:ss}";
        return GrowcubeFrame.EncodeStandard(44, payload);
    }

    public static byte[] SetWorkMode(GrowcubeWorkMode mode)
    {
        return GrowcubeFrame.EncodeStandard(43, ((int)mode).ToString());
    }

    public static byte[] Water(GrowcubeChannel channel, bool start)
    {
        return GrowcubeFrame.EncodeStandard(47, $"{(int)channel}@{(start ? 1 : 0)}");
    }

    public static byte[] ClosePump(GrowcubeChannel channel)
    {
        return GrowcubeFrame.EncodeStandard(46, ((int)channel).ToString());
    }

    public static byte[] PlantEnd(GrowcubeChannel channel)
    {
        return GrowcubeFrame.EncodeStandard(45, ((int)channel).ToString());
    }

    public static byte[] RequestCurveData(GrowcubeChannel channel)
    {
        return GrowcubeFrame.EncodeStandard(48, ((int)channel).ToString());
    }

    // payload: channel@mode@v1@v2 per notes :contentReference[oaicite:9]{index=9}
    public static byte[] SetWaterMode(GrowcubeChannel channel, WateringMode mode, string v1, string v2)
    {
        return GrowcubeFrame.EncodeStandard(49, $"{(int)channel}@{(int)mode}@{v1}@{v2}");
    }

    public static byte[] WifiSettings(string ssid, string password, long timeMils)
    {
        return GrowcubeFrame.EncodeWifiSettings(ssid, password, timeMils);
    }
}