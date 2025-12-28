using System.Text;

namespace Bergdahl.Growcube.Client.Protocol;

/// <summary>
/// Represents a data frame sent to or received from a Growcube.
/// </summary>
/// <param name="Opcode">The opcode of the frame.</param>
/// <param name="Payload">The payload string of the frame.</param>
/// <param name="Raw">The raw byte data of the frame.</param>
public sealed record GrowcubeFrame(int Opcode, string Payload, byte[] Raw)
{
    /// <summary>
    /// The standard frame header.
    /// </summary>
    public const string Header = "elea";

    /// <summary>
    /// The delimiter used in standard frames.
    /// </summary>
    public const char Hash = '#';

    /// <summary>
    /// Encodes a standard command into a byte array.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="payload">The payload string.</param>
    /// <returns>The encoded byte array.</returns>
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

    /// <summary>
    /// Encodes WiFi settings into the special format required by the device.
    /// </summary>
    /// <param name="ssid">The WiFi SSID.</param>
    /// <param name="password">The WiFi password.</param>
    /// <param name="timeMils">The current time in milliseconds (Unix timestamp).</param>
    /// <returns>The encoded byte array.</returns>
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