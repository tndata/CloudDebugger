using CloudDebugger.Infrastructure.OpenTelemetry;
using global::OpenTelemetry.Logs;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace CloudDebugger.Features.McpServer;

[McpServerResourceType]
public static class McpLogResources
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [McpServerResource(UriTemplate = "logs://service/CloudDebugger/app.log", Name = "Application Logs", MimeType = "application/json")]
    [Description("Latest 50 application log entries from OpenTelemetry")]
    public static TextResourceContents GetApplicationLogs()
    {
        try
        {
            var allLogs = OpenTelemetryObserver.LogItemsLog;
            var logEntries = allLogs
                .OrderByDescending(log => log.Timestamp)
                .Take(50)
                .Select(CreateLogEntry)
                .ToList();

            var result = CreateLogResult(
                filterType: "latest",
                filterValue: "50",
                logEntries: logEntries,
                totalAvailable: allLogs.Count
            );

            return CreateResponse(result, "logs://service/CloudDebugger/app.log");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, "logs://service/CloudDebugger/app.log");
        }
    }

    [McpServerResource(UriTemplate = "logs://service/CloudDebugger/app.log/{traceId}", Name = "Application Logs by Trace ID", MimeType = "application/json")]
    [Description("Application log entries filtered by a specific trace ID")]
    public static TextResourceContents GetApplicationLogsByTraceId(RequestContext<ReadResourceRequestParams> requestContext, string traceId)
    {
        var uri = requestContext.Params?.Uri ?? $"logs://service/CloudDebugger/app.log/{traceId}";

        try
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return CreateErrorResponse(
                    new ArgumentException("Trace ID cannot be empty"),
                    uri,
                    new { provided_trace_id = traceId }
                );
            }

            // Parse trace ID (handle both 32-char hex and GUID formats)
            var parsedTraceId = ParseTraceId(traceId);

            var allLogs = OpenTelemetryObserver.LogItemsLog;
            var logEntries = allLogs
                .Where(log => log.TraceId == parsedTraceId)
                .OrderBy(log => log.Timestamp)
                .Select(CreateLogEntry)
                .ToList();

            var result = CreateLogResult(
                filterType: "trace_id",
                filterValue: traceId,
                logEntries: logEntries,
                totalAvailable: allLogs.Count,
                additionalInfo: new { trace_id_parsed = parsedTraceId.ToString() }
            );

            return CreateResponse(result, uri);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, uri, new { provided_trace_id = traceId });
        }
    }

    private static object CreateLogEntry(LogRecord logRecord)
    {
        return new
        {
            timestamp = logRecord.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            level = logRecord.LogLevel.ToString(),
            category = logRecord.CategoryName ?? "Unknown",
            message = logRecord.FormattedMessage ?? logRecord.Body?.ToString() ?? "",
            exception = logRecord.Exception?.ToString(),
            exception_type = logRecord.Exception?.GetType().Name,
            trace_id = logRecord.TraceId.ToString(),
            span_id = logRecord.SpanId.ToString(),
            event_id = logRecord.EventId.Id,
            event_name = logRecord.EventId.Name,
            attributes = GetLogAttributes(logRecord)
        };
    }

    private static Dictionary<string, object> CreateLogResult(
        string filterType,
        string filterValue,
        List<object> logEntries,
        int totalAvailable,
        object? additionalInfo = null)
    {
        var result = new Dictionary<string, object>
        {
            ["service"] = "CloudDebugger",
            ["log_file"] = "app.log",
            ["filter_type"] = filterType,
            ["filter_value"] = filterValue,
            ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["total_entries"] = logEntries.Count,
            ["total_available_entries"] = totalAvailable,
            ["logs"] = logEntries
        };

        // Merge additional info if provided
        if (additionalInfo != null)
        {
            foreach (var prop in additionalInfo.GetType().GetProperties())
            {
                var value = prop.GetValue(additionalInfo);
                if (value != null)
                {
                    // Ensure we only add non-null values
                    result[prop.Name] = value;
                }
            }
        }

        return result;
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

    private static TextResourceContents CreateErrorResponse(
        Exception ex,
        string uri,
        object? additionalInfo = null)
    {
        var errorResult = new Dictionary<string, object>
        {
            ["service"] = "CloudDebugger",
            ["error"] = true,
            ["error_message"] = ex.Message,
            ["exception_type"] = ex.GetType().Name,
            ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        // Merge additional info if provided
        if (additionalInfo != null)
        {
            foreach (var prop in additionalInfo.GetType().GetProperties())
            {
                var value = prop.GetValue(additionalInfo);
                if (value != null)
                {
                    errorResult[prop.Name] = value;
                }
            }
        }

        return CreateResponse(errorResult, uri);
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

    private static Dictionary<string, object?> GetLogAttributes(LogRecord logRecord)
    {
        var attributes = new Dictionary<string, object?>();

        if (logRecord.Attributes != null)
        {
            foreach (var attribute in logRecord.Attributes)
            {
                attributes[attribute.Key] = attribute.Value;
            }
        }

        return attributes;
    }
}