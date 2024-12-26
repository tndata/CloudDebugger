using CloudDebugger.Infrastructure.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace CloudDebugger.Features.OpenTelemetryTracesViewer;

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
public class OpenTelemetryTracesViewerController : Controller
{
    public OpenTelemetryTracesViewerController()
    {
    }

    public IActionResult ViewTraces()
    {
        var model = new ViewTracesModel()
        {
            Entries = RenderTraceData(OpenTelemetryObserver.TraceItemsLog)
        };

        return View(model);
    }

    /// <summary>
    /// Clear the OpenTelemetry trace log
    /// </summary>
    public IActionResult ClearTraceLog()
    {
        OpenTelemetryObserver.TraceItemsLog.Clear();

        return RedirectToAction("ViewTraces");
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
    /// We ignore tags, as it just contains the string representation of TagObjects
    /// </summary>
    /// <param name="traceLog"></param>
    /// <returns></returns>
    private static List<TraceEntry> RenderTraceData(ICollection<Activity> traceLog)
    {
        var result = new List<TraceEntry>();

        foreach (var entry in traceLog)
        {
            var sb = new StringBuilder();

            sb.AppendLine("== Activity Info ==");
            sb.AppendLine($"Name:       {entry.DisplayName}");
            sb.AppendLine($"ID:         {entry.Id}");
            sb.AppendLine($"Parent ID:  {entry.ParentId}");
            sb.AppendLine($"Trace ID:   {entry.TraceId}");
            sb.AppendLine($"Span ID:    {entry.SpanId}");
            sb.AppendLine($"Start Time: {entry.StartTimeUtc:O}");
            sb.AppendLine($"Duration:   {entry.Duration.TotalMilliseconds} ms");

            sb.AppendLine($"Status:             {entry.Status} {entry.StatusDescription}");
            sb.AppendLine($"Recorded:           {entry.Recorded}");
            sb.AppendLine($"IsAllDataRequested: {entry.IsAllDataRequested}");
            sb.AppendLine($"IsStopped:          {entry.IsStopped}");
            sb.AppendLine($"OperationName :     {entry.OperationName}");

            RenderTraceBagage(entry, sb);

            RenderTagObjects(entry, sb);

            RenderTraceEvents(entry, sb);

            RenderTraceLinks(entry, sb);

            if (!IgnoreEntry(entry))
            {
                result.Add(new TraceEntry()
                {
                    Time = entry.StartTimeUtc,
                    Subject = $"{entry.DisplayName} ({entry.Id})",
                    Data = sb.ToString()
                });
            }
        }

        return result;
    }

    private static void RenderTraceLinks(Activity entry, StringBuilder sb)
    {
        if (entry.Links != null && entry.Links.Any())
        {
            sb.AppendLine("== Links ==");
            foreach (var link in entry.Links)
            {
                sb.AppendLine($"  Trace ID: {link.Context.TraceId}, Span ID: {link.Context.SpanId}");
                if (link.Tags != null && link.Tags.Any())
                {
                    foreach (var tag in link.Tags)
                    {
                        sb.AppendLine($"    {tag.Key}: {tag.Value}");
                    }
                }
            }
        }
    }

    private static void RenderTraceEvents(Activity entry, StringBuilder sb)
    {
        if (entry.Events != null && entry.Events.Any())
        {
            sb.AppendLine("== Events ==");
            foreach (var activityEvent in entry.Events)
            {
                sb.AppendLine($"  Event: {activityEvent.Name} at {activityEvent.Timestamp:O}");
                foreach (var attribute in activityEvent.Tags)
                {
                    sb.AppendLine($"    {attribute.Key}: {attribute.Value}");
                }
            }
        }
    }

    private static void RenderTagObjects(Activity entry, StringBuilder sb)
    {
        if (entry.TagObjects.Any())
        {
            sb.AppendLine("");
            sb.AppendLine("== TagObjects ==");

            foreach (var tag in entry.TagObjects)
            {
                sb.AppendLine($"{tag.Key} {tag.Value}");
            }
            sb.AppendLine("");
        }
    }

    private static void RenderTraceBagage(Activity entry, StringBuilder sb)
    {
        if (entry.Baggage != null && entry.Baggage.Any())
        {
            sb.AppendLine("");
            sb.AppendLine("== Baggage ==");

            foreach (var bag in entry.Baggage)
            {
                sb.AppendLine($"{bag.Key} {bag.Value}");
            }
            sb.AppendLine("");
        }
    }


    /// <summary>
    /// We ignore the following trace entries
    /// 
    /// /*/getScriptTag
    /// /OpenTelemetry/*
    /// /_vs/*
    /// /_framework/*
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    private static bool IgnoreEntry(Activity entry)
    {
        var fullUrl = entry.Tags.FirstOrDefault(tag => tag.Key == "url.full").Value;

        if (!string.IsNullOrEmpty(fullUrl) && fullUrl.Contains("/getScriptTag"))
            return true;

        var urlPath = entry.Tags.FirstOrDefault(tag => tag.Key == "url.path").Value;
        if (!string.IsNullOrEmpty(urlPath))
        {
            if (urlPath.StartsWith("/_vs"))
                return true;
            if (urlPath.StartsWith("/_framework"))
                return true;
            if (urlPath.StartsWith("/OpenTelemetry"))
                return true;
        }

        return false;
    }
}