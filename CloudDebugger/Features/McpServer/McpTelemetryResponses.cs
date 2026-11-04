using System.Text.Json.Serialization;

namespace CloudDebugger.Features.McpServer;

internal sealed record McpTraceEntry(
    [property: JsonPropertyName("trace_id")] string TraceId,
    [property: JsonPropertyName("span_id")] string SpanId,
    [property: JsonPropertyName("parent_span_id")] string ParentSpanId,
    [property: JsonPropertyName("operation_name")] string OperationName,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("duration_ms")] double DurationMs,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("tags")] IReadOnlyDictionary<string, object?> Tags);

internal sealed record McpTracesResponse(
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("traces")] IReadOnlyList<McpTraceEntry> Traces,
    [property: JsonPropertyName("trace_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId = null)
{
    [JsonPropertyName("service")]
    public string Service { get; } = "CloudDebugger";

    [JsonPropertyName("count")]
    public int Count => Traces.Count;
}

internal sealed record McpTraceErrorResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("timestamp")] string Timestamp)
{
    [JsonPropertyName("service")]
    public string Service { get; } = "CloudDebugger";

    [JsonPropertyName("error")]
    public bool Error { get; } = true;
}

internal sealed record McpLogEntry(
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("exception")] string? Exception,
    [property: JsonPropertyName("exception_type")] string? ExceptionType,
    [property: JsonPropertyName("trace_id")] string TraceId,
    [property: JsonPropertyName("span_id")] string SpanId,
    [property: JsonPropertyName("event_id")] int EventId,
    [property: JsonPropertyName("event_name")] string? EventName,
    [property: JsonPropertyName("attributes")] IReadOnlyDictionary<string, object?> Attributes);

internal sealed record McpLogsResponse(
    [property: JsonPropertyName("filter_type")] string FilterType,
    [property: JsonPropertyName("filter_value")] string FilterValue,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("total_available_entries")] int TotalAvailableEntries,
    [property: JsonPropertyName("logs")] IReadOnlyList<McpLogEntry> Logs,
    [property: JsonPropertyName("trace_id_parsed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceIdParsed = null)
{
    [JsonPropertyName("service")]
    public string Service { get; } = "CloudDebugger";

    [JsonPropertyName("log_file")]
    public string LogFile { get; } = "app.log";

    [JsonPropertyName("total_entries")]
    public int TotalEntries => Logs.Count;
}

internal sealed record McpLogErrorResponse(
    [property: JsonPropertyName("error_message")] string ErrorMessage,
    [property: JsonPropertyName("exception_type")] string ExceptionType,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("provided_trace_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProvidedTraceId = null)
{
    [JsonPropertyName("service")]
    public string Service { get; } = "CloudDebugger";

    [JsonPropertyName("error")]
    public bool Error { get; } = true;
}
