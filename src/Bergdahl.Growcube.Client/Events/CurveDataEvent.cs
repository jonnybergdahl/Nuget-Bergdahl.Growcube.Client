namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event containing curve data for a specific channel and date.
/// </summary>
/// <param name="Channel">The channel the data belongs to.</param>
/// <param name="Date">The date of the data.</param>
/// <param name="Values">The data values.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record CurveDataEvent(
    GrowcubeChannel Channel,
    DateOnly Date,
    IReadOnlyList<int> Values,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);