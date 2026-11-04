using CloudDebugger.Infrastructure.OpenTelemetry;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace CloudDebugger.Features.McpServer;

[McpServerResourceType]
public static class McpTraceResources
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    [McpServerResource(UriTemplate = "traces://service/CloudDebugger", Name = "All Traces", MimeType = "application/json")]
    [Description("All available trace entries from OpenTelemetry activities")]
    public static TextResourceContents GetAllTraces()
    {
        try
        {
            var traces = OpenTelemetryObserver.TraceItemsLog
                .OrderByDescending(a => a.StartTimeUtc)
                .Select(activity => MapActivityToTrace(activity))
                .ToList();

            McpTracesResponse result = new(
                Timestamp: DateTime.UtcNow.ToString(DateTimeFormat),
                Traces: traces);

            return CreateResponse(result, "traces://service/CloudDebugger");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, "traces://service/CloudDebugger");
        }
    }

    [McpServerResource(UriTemplate = "traces://service/CloudDebugger/{traceId}", Name = "Trace by ID", MimeType = "application/json")]
    [Description("Trace entries filtered by a specific trace ID")]
    public static TextResourceContents GetTraceById(RequestContext<ReadResourceRequestParams> requestContext, string traceId)
    {
        var uri = requestContext.Params?.Uri ?? $"traces://service/CloudDebugger/{traceId}";

        try
        {
            ActivityTraceId parsedTraceId = McpTraceIdParser.Parse(traceId);

            var traces = OpenTelemetryObserver.TraceItemsLog
                .Where(activity => activity.TraceId == parsedTraceId)
                .OrderBy(a => a.StartTimeUtc)
                .Select(activity => MapActivityToTrace(activity))
                .ToList();

            McpTracesResponse result = new(
                Timestamp: DateTime.UtcNow.ToString(DateTimeFormat),
                Traces: traces,
                TraceId: traceId);

            return CreateResponse(result, uri);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, uri);
        }
    }

    private static McpTraceEntry MapActivityToTrace(Activity activity)
    {
        Dictionary<string, object?> tags = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> tag in activity.TagObjects)
        {
            // Activity permits repeated names; retain the last value in the existing JSON object.
            tags[tag.Key] = tag.Value;
        }

        return new McpTraceEntry(
            TraceId: activity.TraceId.ToString(),
            SpanId: activity.SpanId.ToString(),
            ParentSpanId: activity.ParentSpanId.ToString(),
            OperationName: activity.DisplayName ?? activity.OperationName ?? "Unknown",
            StartTime: activity.StartTimeUtc.ToString(DateTimeFormat),
            DurationMs: activity.Duration.TotalMilliseconds,
            Status: activity.Status.ToString(),
            Kind: activity.Kind.ToString(),
            Source: activity.Source?.Name,
            Tags: tags);
    }

    private static TextResourceContents CreateResponse<T>(T data, string uri)
    {
        return new TextResourceContents
        {
            Text = JsonSerializer.Serialize(data, JsonOptions),
            MimeType = "application/json",
            Uri = uri
        };
    }

    private static TextResourceContents CreateErrorResponse(Exception ex, string uri)
    {
        McpTraceErrorResponse error = new(
            Message: ex.Message,
            Type: ex.GetType().Name,
            Timestamp: DateTime.UtcNow.ToString(DateTimeFormat));

        return CreateResponse(error, uri);
    }
}
