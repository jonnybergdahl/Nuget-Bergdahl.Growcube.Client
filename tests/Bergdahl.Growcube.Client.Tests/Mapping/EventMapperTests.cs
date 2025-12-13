using Bergdahl.Growcube.Client.Events;
using Bergdahl.Growcube.Client.Internal;
using Bergdahl.Growcube.Client.Protocol;
using Xunit;

namespace Bergdahl.Growcube.Client.Tests.Mapping;

public sealed class EventMapperTests
{
    [Fact]
    public void Map_ParsesCurveData()
    {
        var frame = new GrowcubeFrame(
            22,
            "0@2023@7@12@00,01,02,03",
            Array.Empty<byte>());

        var ev = EventMapper.Map(frame, DateTimeOffset.UnixEpoch);

        var curve = Assert.IsType<CurveDataEvent>(ev);
        Assert.Equal(GrowcubeChannel.A, curve.Channel);
        Assert.Equal(new DateOnly(2023, 7, 12), curve.Date);
        Assert.Equal(new[] { 0, 1, 2, 3 }, curve.Values);
    }

    [Fact]
    public void Map_ParsesAutoWaterTimestamp()
    {
        var frame = new GrowcubeFrame(
            23,
            "1@2023@7@12@22@49",
            Array.Empty<byte>());

        var ev = EventMapper.Map(frame, DateTimeOffset.UnixEpoch);

        var tsEv = Assert.IsType<AutoWaterTimestampEvent>(ev);
        Assert.Equal(GrowcubeChannel.B, tsEv.Channel);
        Assert.Equal(2023, tsEv.When.Year);
        Assert.Equal(7, tsEv.When.Month);
        Assert.Equal(12, tsEv.When.Day);
        Assert.Equal(22, tsEv.When.Hour);
        Assert.Equal(49, tsEv.When.Minute);
    }
}