namespace Bergdahl.Growcube.Client.Events;

/// <summary>
/// Event for unknown or unhandled opcodes.
/// </summary>
/// <param name="Opcode">The raw opcode.</param>
/// <param name="RawPayload">The raw payload string.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public sealed record UnknownEvent(
    int Opcode,
    string RawPayload,
    DateTimeOffset Timestamp) : GrowcubeEvent(Timestamp);