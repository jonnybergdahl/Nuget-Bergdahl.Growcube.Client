using System.Text;

namespace Bergdahl.Growcube.Client.Protocol;

public sealed record GrowcubeFrame(int Opcode, string Payload, byte[] Raw)
{
    public const string Header = "elea";
    public const char Hash = '#';

    public static byte[] EncodeStandard(int opcode, string payload)
    {
        if (opcode is < 0 or > 99) throw new ArgumentOutOfRangeException(nameof(opcode));
        payload ??= string.Empty;

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var cmd = opcode.ToString("00");
        var len = payloadBytes.Length.ToString();

        // eleaXX#NN#<payload>#
        var prefix = Encoding.ASCII.GetBytes($"{Header}{cmd}{Hash}{len}{Hash}");
        var suffix = new[] { (byte)Hash };

        var result = new byte[prefix.Length + payloadBytes.Length + suffix.Length];
        Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
        Buffer.BlockCopy(payloadBytes, 0, result, prefix.Length, payloadBytes.Length);
        Buffer.BlockCopy(suffix, 0, result, prefix.Length + payloadBytes.Length, suffix.Length);
        return result;
    }

    // Special-case WiFi settings format documented in your notes: "elea50]*{len}]*{ssid}'{pwd}'{timeMils}]*" :contentReference[oaicite:2]{index=2}
    public static byte[] EncodeWifiSettings(string ssid, string password, long timeMils)
    {
        ssid ??= "";
        password ??= "";

        var content = $"{ssid}'{password}'{timeMils}";
        var len = Encoding.UTF8.GetByteCount(content);

        var msg = $"elea50]*{len}]*{content}]*";
        return Encoding.UTF8.GetBytes(msg);
    }
}