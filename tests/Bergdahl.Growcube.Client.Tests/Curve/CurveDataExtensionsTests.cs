using Bergdahl.Growcube.Client.Curve;
using Bergdahl.Growcube.Client.Events;
using Xunit;

namespace Bergdahl.Growcube.Client.Tests.Curve;

public sealed class CurveDataExtensionsTests
{
    [Fact]
    public void ToTimeSeries_DropsTrailingZeros_WhenEnabled()
    {
        var day = new CurveDataEvent(
            Bergdahl.Growcube.Client.GrowcubeChannel.A,
            new DateOnly(2023, 7, 12),
            new[] { 1, 2, 0, 0 },
            DateTimeOffset.UnixEpoch);

        var series = new[] { day }.ToTimeSeries(TimeSpan.FromMinutes(60), dropTrailingZeros: true);

        Assert.Equal(2, series.Count);
        Assert.Equal(1, series[0].Value);
        Assert.Equal(2, series[1].Value);
    }

    [Fact]
    public void DeduplicateByDate_ChoosesHighestValueCount_ByDefault()
    {
        var d1 = new CurveDataEvent(Bergdahl.Growcube.Client.GrowcubeChannel.A, new DateOnly(2023, 7, 12), new[] { 1 }, DateTimeOffset.UnixEpoch);
        var d2 = new CurveDataEvent(Bergdahl.Growcube.Client.GrowcubeChannel.A, new DateOnly(2023, 7, 12), new[] { 1, 2, 3 }, DateTimeOffset.UnixEpoch);

        var deduped = new[] { d1, d2 }.DeduplicateByDate();

        Assert.Single(deduped);
        Assert.Equal(3, deduped[0].Values.Count);
    }
}