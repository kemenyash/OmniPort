using Microsoft.Extensions.Logging;

namespace Web.Telemetry
{
    public sealed record ApplicationLogEntry(
        long Id,
        DateTimeOffset Timestamp,
        LogLevel Level,
        string Category,
        int EventId,
        string? EventName,
        string Message,
        string? Exception,
        string? TraceId,
        string? SpanId,
        IReadOnlyDictionary<string, string> Scopes);
}
