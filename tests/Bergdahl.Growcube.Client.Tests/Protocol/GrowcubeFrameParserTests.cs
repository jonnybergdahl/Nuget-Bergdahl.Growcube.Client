using System.Text;
using Bergdahl.Growcube.Client.Protocol;
using Xunit;

namespace Bergdahl.Growcube.Client.Tests.Protocol;

public sealed class GrowcubeFrameParserTests
{
    [Fact]
    public void Drain_ParsesTwoConcatenatedFrames_WithPaddingZeros()
    {
        var f1 = GrowcubeFrame.EncodeStandard(24, "3.6@12663500");
        var f2 = GrowcubeFrame.EncodeStandard(21, "0@26@45@25");

        var bytes = new byte[f1.Length + f2.Length + 4];
        Buffer.BlockCopy(f1, 0, bytes, 0, f1.Length);
        Buffer.BlockCopy(f2, 0, bytes, f1.Length, f2.Length);
        // trailing padding zeros
        bytes[^1] = 0x00; bytes[^2] = 0x00; bytes[^3] = 0x00; bytes[^4] = 0x00;

        var p = new GrowcubeFrameParser();
        p.Append(bytes);

        var frames = p.Drain().ToArray();
        Assert.Equal(2, frames.Length);

        Assert.Equal(24, frames[0].Opcode);
        Assert.Equal("3.6@12663500", frames[0].Payload);

        Assert.Equal(21, frames[1].Opcode);
        Assert.Equal("0@26@45@25", frames[1].Payload);
    }

    [Fact]
    public void Drain_HandlesPartialFrameAcrossAppends()
    {
        var f = GrowcubeFrame.EncodeStandard(20, "1");

        var p = new GrowcubeFrameParser();
        p.Append(f.AsSpan(0, 3)); // incomplete
        Assert.Empty(p.Drain());

        p.Append(f.AsSpan(3));   // remainder
        var frames = p.Drain().ToArray();

        Assert.Single(frames);
        Assert.Equal(20, frames[0].Opcode);
        Assert.Equal("1", frames[0].Payload);
    }

    [Fact]
    public void Drain_SkipsLeadingGarbageBeforeHeader()
    {
        var garbage = Encoding.ASCII.GetBytes("xxxx");
        var f = GrowcubeFrame.EncodeStandard(33, "0@0");

        var bytes = new byte[garbage.Length + f.Length];
        Buffer.BlockCopy(garbage, 0, bytes, 0, garbage.Length);
        Buffer.BlockCopy(f, 0, bytes, garbage.Length, f.Length);

        var p = new GrowcubeFrameParser();
        p.Append(bytes);

        var frames = p.Drain().ToArray();
        Assert.Single(frames);
        Assert.Equal(33, frames[0].Opcode);
        Assert.Equal("0@0", frames[0].Payload);
    }
}
