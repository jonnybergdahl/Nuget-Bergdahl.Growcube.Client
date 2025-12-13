using System.Text;

namespace Bergdahl.Growcube.Client.Protocol;

public sealed class GrowcubeFrameParser
{
    private readonly List<byte> _buffer = new();

    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return;
        _buffer.AddRange(data.ToArray());
    }

    public IEnumerable<GrowcubeFrame> Drain()
    {
        while (true)
        {
            var frame = TryParseOne(out var consumed);

            if (consumed > 0)
                _buffer.RemoveRange(0, consumed);

            if (frame is not null)
            {
                yield return frame;
                continue;
            }

            // No frame parsed. If we consumed something, we made progress (e.g., dropped garbage),
            // so loop again. If we consumed nothing, we need more data.
            if (consumed == 0)
                yield break;
        }
    }


    private GrowcubeFrame? TryParseOne(out int consumed)
    {
        consumed = 0;

        if (_buffer.Count < 6) return null;

        // Find "elea"
        var start = IndexOfAscii(_buffer, GrowcubeFrame.Header);
        if (start < 0)
        {
            // No header: drop garbage/padding
            consumed = _buffer.Count;
            return null;
        }

        if (start > 0)
        {
            // Drop leading garbage
            consumed = start;
            return null;
        }

        // Need "elea" + 2 opcode digits at least
        if (_buffer.Count < 6) return null;

        // Parse opcode (two ASCII digits) at offset 4
        if (!IsDigit(_buffer[4]) || !IsDigit(_buffer[5]))
        {
            consumed = 4; // drop "elea" and resync
            return null;
        }

        var opcode = (_buffer[4] - (byte)'0') * 10 + (_buffer[5] - (byte)'0');

        // Special format for opcode 50: "elea50]*...]*...]*" :contentReference[oaicite:4]{index=4}
        if (opcode == 50)
            return TryParseWifi50(out consumed);

        // Standard format: eleaXX#LEN#PAYLOAD# :contentReference[oaicite:5]{index=5}
        var idx = 6;
        if (_buffer.Count <= idx) return null;
        if (_buffer[idx] != (byte)GrowcubeFrame.Hash)
        {
            consumed = 4;
            return null;
        }

        idx++;

        // Read LEN digits until '#'
        var lenStart = idx;
        while (idx < _buffer.Count && _buffer[idx] != (byte)GrowcubeFrame.Hash)
        {
            if (!IsDigit(_buffer[idx]))
            {
                consumed = 4;
                return null;
            }

            idx++;
        }

        if (idx >= _buffer.Count) return null; // incomplete
        var lenStr = Encoding.ASCII.GetString(_buffer.GetRange(lenStart, idx - lenStart).ToArray());
        if (!int.TryParse(lenStr, out var payloadLen) || payloadLen < 0)
        {
            consumed = 4;
            return null;
        }

        idx++; // skip '#'

        // Ensure we have payload + final '#'
        var needed = idx + payloadLen + 1;
        if (_buffer.Count < needed) return null;

        var payloadBytes = _buffer.GetRange(idx, payloadLen).ToArray();
        var terminator = _buffer[idx + payloadLen];
        if (terminator != (byte)GrowcubeFrame.Hash)
        {
            consumed = 4;
            return null;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var raw = _buffer.GetRange(0, needed).ToArray();

        consumed = needed;

        // Also consume any immediate padding zeros after the frame (common in captures) :contentReference[oaicite:6]{index=6}
        while (consumed < _buffer.Count && _buffer[consumed] == 0x00)
            consumed++;

        return new GrowcubeFrame(opcode, payload, raw);
    }

    private GrowcubeFrame? TryParseWifi50(out int consumed)
    {
        consumed = 0;

        var prefix = Encoding.ASCII.GetBytes("elea50]*");
        if (_buffer.Count < prefix.Length) return null;

        for (var i = 0; i < prefix.Length; i++)
            if (_buffer[i] != prefix[i])
            {
                consumed = 4;
                return null;
            }

        var lenEnd = IndexOfBytes(_buffer, Encoding.ASCII.GetBytes("]*"), prefix.Length);
        if (lenEnd < 0) return null;

        var lenStr = Encoding.ASCII.GetString(_buffer.GetRange(prefix.Length, lenEnd - prefix.Length).ToArray());
        if (!int.TryParse(lenStr, out var contentLen) || contentLen < 0)
        {
            consumed = 4;
            return null;
        }

        var contentStart = lenEnd + 2;
        var trailerIndex = contentStart + contentLen;

        if (_buffer.Count < trailerIndex + 2) return null;
        if (_buffer[trailerIndex] != (byte)']' || _buffer[trailerIndex + 1] != (byte)'*')
            return null;

        var payloadBytes = _buffer.GetRange(contentStart, contentLen).ToArray();
        var payload = Encoding.UTF8.GetString(payloadBytes);

        consumed = trailerIndex + 2;
        var raw = _buffer.GetRange(0, consumed).ToArray();

        // Consume padding zeros after frame
        while (consumed < _buffer.Count && _buffer[consumed] == 0x00)
            consumed++;

        return new GrowcubeFrame(50, payload, raw);
    }

    private static int IndexOfAscii(List<byte> haystack, string needleAscii)
    {
        return IndexOfBytes(haystack, Encoding.ASCII.GetBytes(needleAscii), 0);
    }

    private static int IndexOfBytes(List<byte> haystack, byte[] needle, int startIndex)
    {
        for (var i = startIndex; i <= haystack.Count - needle.Length; i++)
        {
            var ok = true;
            for (var j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j])
                {
                    ok = false;
                    break;
                }

            if (ok) return i;
        }

        return -1;
    }

    private static bool IsDigit(byte b)
    {
        return b >= (byte)'0' && b <= (byte)'9';
    }
}