using CloudDebugger.Infrastructure.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using System.Text;

namespace CloudDebugger.Features.OpenTelemetryLogViewer;

/// <summary>
/// Controller for handling OpenTelemetry operations.
/// 
/// The Azure Monitor Distro client SDK Documentation
/// https://github.com/Azure/azure-sdk-for-net/tree/Azure.Monitor.OpenTelemetry.AspNetCore_1.2.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore 
/// 
/// HttpClient and HttpWebRequest instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http
/// 
/// SqlClient Instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.SqlClient
/// </summary>
public class OpenTelemetryLogViewerController : Controller
{
    public OpenTelemetryLogViewerController()
    {
    }

    public IActionResult ViewLogs()
    {
        var model = new ViewLogsModel()
        {
            Entries = RenderLogData(OpenTelemetryObserver.LogItemsLog)
        };

        return View(model);
    }



    /// <summary>
    /// Clear the OpenTelemetry Logs log
    /// </summary>
    /// <returns></returns>
    public IActionResult ClearLogs()
    {
        OpenTelemetryObserver.LogItemsLog.Clear();

        return RedirectToAction("ViewLogs");
    }


    /// <summary>
    /// Log record source:
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Logs/LogRecord.cs 
    /// </summary>
    /// <param name="logItemsLog"></param>
    /// <returns></returns>
    private static List<LogEntry> RenderLogData(ICollection<LogRecord> logItemsLog)
    {
        var result = new List<LogEntry>();

        foreach (var log in logItemsLog)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"== Trace Context ==");
            sb.AppendLine($"TraceId:      {log.TraceId}");
            sb.AppendLine($"SpanId:       {log.SpanId}");
            sb.AppendLine($"TraceFlags:   {log.TraceFlags}");
            sb.AppendLine($"TraceState:   {log.TraceState}");

            sb.AppendLine();
            sb.AppendLine("== Log Metadata ==");
            sb.AppendLine($"Timestamp:    {log.Timestamp:O}");
            sb.AppendLine($"Category:     {log.CategoryName}");

            if (!string.IsNullOrWhiteSpace(log.FormattedMessage))
            {
                sb.AppendLine();
                sb.AppendLine("== Formatted Message ==");
                sb.AppendLine(log.FormattedMessage);
            }

            if (log.Body != null)
            {
                sb.AppendLine();
                sb.AppendLine("== Body ==");
                sb.AppendLine(log.Body);
            }

            // Don't render StateValues, they are depecrated.

            if (log.Attributes != null && log.Attributes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("== Attributes ==");
                foreach (var attribute in log.Attributes)
                {
                    sb.AppendLine($"- {attribute.Key}: {attribute.Value}");
                }
            }

            if (log.Exception != null)
            {
                sb.AppendLine();
                sb.AppendLine("== Exception ==");
                sb.AppendLine($"Type: {log.Exception.GetType().FullName}");
                sb.AppendLine($"Message: {log.Exception.Message}");
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(log.Exception.StackTrace);
            }

            result.Add(new LogEntry
            {
                Time = log.Timestamp.ToUniversalTime(),
                Subject = log.CategoryName,
                Data = sb.ToString()
            });
        }

        return result;
    }
}