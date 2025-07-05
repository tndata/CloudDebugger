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
    const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";


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

            var result = new
            {
                service = "CloudDebugger",
                timestamp = DateTime.UtcNow.ToString(DateTimeFormat),
                count = traces.Count,
                traces
            };

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
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return CreateErrorResponse(new ArgumentException("Trace ID cannot be empty"), uri);
            }

            // Parse trace ID (handle both 32-char hex and GUID formats)
            var parsedTraceId = ParseTraceId(traceId);

            var traces = OpenTelemetryObserver.TraceItemsLog
                .Where(activity => activity.TraceId == parsedTraceId)
                .OrderBy(a => a.StartTimeUtc)
                .Select(activity => MapActivityToTrace(activity))
                .ToList();

            var result = new
            {
                service = "CloudDebugger",
                trace_id = traceId,
                timestamp = DateTime.UtcNow.ToString(DateTimeFormat),
                count = traces.Count,
                traces
            };

            return CreateResponse(result, uri);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, uri);
        }
    }

    private static object MapActivityToTrace(Activity activity)
    {
        return new
        {
            trace_id = activity.TraceId.ToString(),
            span_id = activity.SpanId.ToString(),
            parent_span_id = activity.ParentSpanId.ToString(),
            operation_name = activity.DisplayName ?? activity.OperationName ?? "Unknown",
            start_time = activity.StartTimeUtc.ToString(DateTimeFormat),
            duration_ms = activity.Duration.TotalMilliseconds,
            status = activity.Status.ToString(),
            kind = activity.Kind.ToString(),
            source = activity.Source?.Name,
            tags = activity.Tags?.ToDictionary(t => t.Key, t => t.Value)
        };
    }

    private static ActivityTraceId ParseTraceId(string traceId)
    {
        // Remove any dashes if present (GUID format)
        var cleanId = traceId.Replace("-", "");

        if (cleanId.Length != 32)
        {
            throw new FormatException($"Invalid trace ID format. Expected 32 hex characters, got {cleanId.Length}");
        }

        return ActivityTraceId.CreateFromString(cleanId.AsSpan());
    }

    private static TextResourceContents CreateResponse(object data, string uri)
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
        var error = new
        {
            service = "CloudDebugger",
            error = true,
            message = ex.Message,
            type = ex.GetType().Name,
            timestamp = DateTime.UtcNow.ToString(DateTimeFormat)
        };

        return CreateResponse(error, uri);
    }
}