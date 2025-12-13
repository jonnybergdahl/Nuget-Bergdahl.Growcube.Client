namespace Bergdahl.Growcube.Client.Events;

/// <summary>
///     Domain-only fallback that still carries opcode + raw payload for forward compatibility.
/// </summary>
public sealed record UnknownEvent(
    int Opcode,
    string RawPayload,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);