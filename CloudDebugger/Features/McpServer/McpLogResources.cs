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
            ActivityTraceId parsedTraceId = McpTraceIdParser.Parse(traceId);

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
                traceIdParsed: parsedTraceId.ToString()
            );

            return CreateResponse(result, uri);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex, uri, traceId);
        }
    }

    private static McpLogEntry CreateLogEntry(LogRecord logRecord)
    {
        return new McpLogEntry(
            Timestamp: logRecord.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Level: logRecord.LogLevel.ToString(),
            Category: logRecord.CategoryName ?? "Unknown",
            Message: logRecord.FormattedMessage ?? logRecord.Body?.ToString() ?? "",
            Exception: logRecord.Exception?.ToString(),
            ExceptionType: logRecord.Exception?.GetType().Name,
            TraceId: logRecord.TraceId.ToString(),
            SpanId: logRecord.SpanId.ToString(),
            EventId: logRecord.EventId.Id,
            EventName: logRecord.EventId.Name,
            Attributes: GetLogAttributes(logRecord));
    }

    private static McpLogsResponse CreateLogResult(
        string filterType,
        string filterValue,
        IReadOnlyList<McpLogEntry> logEntries,
        int totalAvailable,
        string? traceIdParsed = null)
    {
        return new McpLogsResponse(
            FilterType: filterType,
            FilterValue: filterValue,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            TotalAvailableEntries: totalAvailable,
            Logs: logEntries,
            TraceIdParsed: traceIdParsed);
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

    private static TextResourceContents CreateErrorResponse(Exception ex, string uri, string? providedTraceId = null)
    {
        McpLogErrorResponse error = new(
            ErrorMessage: ex.Message,
            ExceptionType: ex.GetType().Name,
            Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ProvidedTraceId: providedTraceId);

        return CreateResponse(error, uri);
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
