using Bergdahl.Growcube.Client.Events;

namespace Bergdahl.Growcube.Client.Curve;

public sealed record CurvePoint(DateTimeOffset Timestamp, int Value);

public static class CurveDataExtensions
{
    /// <summary>
    /// Normalizes daily curve packets into a single time series.
    /// </summary>
    /// <param name="days">Curve packets (usually one per day).</param>
    /// <param name="sampleInterval">
    /// Interval between samples. If unknown, pass what you want to assume (commonly 30 minutes or 1 hour).
    /// </param>
    /// <param name="timeZone">
    /// Time zone to interpret device-local dates. Defaults to <see cref="TimeZoneInfo.Local"/>.
    /// </param>
    /// <param name="dropTrailingZeros">
    /// If true, drops trailing zeros for each day (useful if the device pads the end of the vector).
    /// </param>
    public static IReadOnlyList<CurvePoint> ToTimeSeries(
        this IEnumerable<CurveDataEvent> days,
        TimeSpan sampleInterval,
        TimeZoneInfo? timeZone = null,
        bool dropTrailingZeros = true)
    {
        if (days is null) throw new ArgumentNullException(nameof(days));
        if (sampleInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(sampleInterval));

        timeZone ??= TimeZoneInfo.Local;

        var ordered = days
            .OrderBy(d => d.Date)
            .ToArray();

        var result = new List<CurvePoint>(capacity: ordered.Sum(d => d.Values.Count));

        foreach (var day in ordered)
        {
            var values = day.Values;
            var count = values.Count;

            if (dropTrailingZeros && count > 0)
            {
                var lastNonZero = count - 1;
                while (lastNonZero >= 0 && values[lastNonZero] == 0)
                    lastNonZero--;

                count = Math.Max(0, lastNonZero + 1);
            }

            // Interpret as device-local midnight with provided timeZone.
            var localMidnight = new DateTime(day.Date.Year, day.Date.Month, day.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var offset = timeZone.GetUtcOffset(localMidnight);
            var start = new DateTimeOffset(localMidnight, offset);

            for (var i = 0; i < count; i++)
            {
                var t = start + TimeSpan.FromTicks(sampleInterval.Ticks * i);
                result.Add(new CurvePoint(t, values[i]));
            }
        }

        return result;
    }

    /// <summary>
    /// Convenience: groups curve packets by date and ensures a stable ordering.
    /// Useful when you collect from ListenAsync and may see duplicates.
    /// </summary>
    public static IReadOnlyList<CurveDataEvent> DeduplicateByDate(
        this IEnumerable<CurveDataEvent> days,
        Func<CurveDataEvent, int>? score = null)
    {
        if (days is null) throw new ArgumentNullException(nameof(days));

        score ??= d => d.Values.Count;

        return days
            .GroupBy(d => d.Date)
            .Select(g => g.OrderByDescending(score).First())
            .OrderBy(d => d.Date)
            .ToArray();
    }
}
