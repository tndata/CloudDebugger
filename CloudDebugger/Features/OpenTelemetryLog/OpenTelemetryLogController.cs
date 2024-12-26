using CloudDebugger.Infrastructure.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CloudDebugger.Features.OpenTelemetryLog;

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
public class OpenTelemetryLogController : Controller
{
    public OpenTelemetryLogController()
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

    public IActionResult ViewLogs()
    {
        var model = new ViewLogsModel()
        {
            Entries = RenderLogData(OpenTelemetryObserver.LogItemsLog)
        };

        return View(model);
    }

    public IActionResult ViewMetrics()
    {
        var model = new ViewMetricsModel()
        {
            Entries = RenderMetricsData(OpenTelemetryObserver.MetricItemsLog)
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
    /// Clear the OpenTelemetry metrics log
    /// </summary>
    public IActionResult ClearMetricsLog()
    {
        OpenTelemetryObserver.MetricItemsLog.Clear();

        return RedirectToAction("ViewMetrics");
    }

    /// <summary>
    /// Metrics source code:
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Metrics/Metric.cs 
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Metrics/MetricPoint/MetricPointsAccessor.cs
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/ConsoleMetricExporter.cs
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/Implementation/Serializer/ProtobufOtlpMetricSerializer.cs
    /// </summary>
    /// <param name="metricItemsLog"></param>
    /// <returns></returns>
    private static List<MetricsEntry> RenderMetricsData(ICollection<Metric> metricItemsLog)
    {
        var result = new List<MetricsEntry>();

        foreach (var metric in metricItemsLog)
        {
            var sb = new StringBuilder();

            // Metric Info
            sb.AppendLine("== Metric Info ==");
            sb.AppendLine($"Name:         {metric.Name}");
            sb.AppendLine($"Description:  {metric.Description}");
            sb.AppendLine($"Unit:         {metric.Unit}");
            sb.AppendLine($"Metric Type:  {metric.MetricType}");
            sb.AppendLine($"Temporality:  {metric.Temporality}");

            sb.AppendLine();
            sb.AppendLine("== Meter Info ==");
            sb.AppendLine($"Meter Name:    {metric.MeterName}");
            sb.AppendLine($"Meter Version: {metric.MeterVersion}");

            if (metric.MeterTags?.Any() == true)
            {
                sb.AppendLine();
                sb.AppendLine("== Meter Tags ==");
                foreach (var tag in metric.MeterTags)
                {
                    sb.AppendLine($"  {tag.Key}: {tag.Value}");
                }
            }

            // Add Metric Points
            sb.AppendLine();
            sb.AppendLine("== Data Points ==");
            foreach (ref readonly var point in metric.GetMetricPoints())
            {
                sb.AppendLine($"  Start Time: {point.StartTime.ToUniversalTime():O}");
                sb.AppendLine($"  End Time:   {point.EndTime.ToUniversalTime():O}");

                switch (metric.MetricType)
                {
                    case MetricType.Histogram:
                        RenderHistogram(point, sb);

                        break;
                    case MetricType.ExponentialHistogram:
                        RenderExponentialHistogram(point, sb);

                        break;
                    case MetricType.DoubleGauge:
                        sb.AppendLine($"  Gauge Value: {point.GetGaugeLastValueDouble()}");
                        break;

                    case MetricType.LongGauge:
                        sb.AppendLine($"  Gauge Value: {point.GetGaugeLastValueLong()}");
                        break;

                    case MetricType.DoubleSum:
                    case MetricType.DoubleSumNonMonotonic:
                        sb.AppendLine($"  Sum (Double): {point.GetSumDouble()}");
                        break;

                    case MetricType.LongSum:
                    case MetricType.LongSumNonMonotonic:
                        sb.AppendLine($"  Sum (Long): {point.GetSumLong()}");
                        break;

                    default:
                        sb.AppendLine($"Unknown metric type");
                        break;
                }

                sb.AppendLine();
                sb.AppendLine("  Tags:");
                foreach (var pointTag in point.Tags)
                {
                    sb.AppendLine($"    {pointTag.Key} {pointTag.Value}");
                }

                // Exemplars
                RenderExemplars(sb, point);

                sb.AppendLine();
            }

            result.Add(new MetricsEntry
            {
                Time = DateTime.UtcNow,
                Subject = metric.Name,
                Data = sb.ToString()
            });
        }

        return result;
    }

    private static void RenderExemplars(StringBuilder sb, MetricPoint point)
    {
        if (point.TryGetExemplars(out var exemplars))
        {
            sb.AppendLine("  Exemplars:");
            foreach (ref readonly var exemplar in exemplars)
            {
                sb.AppendLine($"    Timestamp: {exemplar.Timestamp:O}");
                sb.AppendLine($"    Value: {exemplar.DoubleValue}");
                if (exemplar.TraceId != default)
                {
                    sb.AppendLine($"    TraceId: {exemplar.TraceId.ToHexString()}");
                    sb.AppendLine($"    SpanId: {exemplar.SpanId.ToHexString()}");
                }

                foreach (var filteredTag in exemplar.FilteredTags)
                {
                    sb.AppendLine($"      {filteredTag.Key}: {filteredTag.Value}");
                }
            }
        }
    }

    private static void RenderExponentialHistogram(MetricPoint point, StringBuilder sb)
    {
        sb.AppendLine($"  Sum:   {point.GetHistogramSum()}");
        sb.AppendLine($"  Count: {point.GetHistogramCount()}");
        if (point.TryGetHistogramMinMaxValues(out var min2, out var max2))
        {
            sb.AppendLine($"  Min:   {min2}");
            sb.AppendLine($"  Max:   {max2}");
        }

        var exponentialHistogramData = point.GetExponentialHistogramData();
        var scale = exponentialHistogramData.Scale;

        if (exponentialHistogramData.ZeroCount != 0)
        {
            sb.AppendLine($"Zero Bucket:{exponentialHistogramData.ZeroCount}");
        }

        var offset = exponentialHistogramData.PositiveBuckets.Offset;
        foreach (var bucketCount in exponentialHistogramData.PositiveBuckets)
        {
            var lowerBound = Base2ExponentialBucketHistogramHelper.CalculateLowerBoundary(offset, scale).ToString(CultureInfo.InvariantCulture);
            var upperBound = Base2ExponentialBucketHistogramHelper.CalculateLowerBoundary(++offset, scale).ToString(CultureInfo.InvariantCulture);
            sb.AppendLine($"({lowerBound}, {upperBound}]:{bucketCount}");
        }
    }

    private static void RenderHistogram(MetricPoint point, StringBuilder sb)
    {
        sb.AppendLine($"  Sum:   {point.GetHistogramSum()}");
        sb.AppendLine($"  Count: {point.GetHistogramCount()}");
        if (point.TryGetHistogramMinMaxValues(out var min, out var max))
        {
            sb.AppendLine($"  Min:   {min}");
            sb.AppendLine($"  Max:   {max}");
        }

        bool isFirstIteration = true;
        double previousExplicitBound = default;
        foreach (var histogramMeasurement in point.GetHistogramBuckets())
        {
            if (isFirstIteration)
            {
                sb.Append("(-Infinity,");
                sb.Append(histogramMeasurement.ExplicitBound);
                sb.Append(']');
                sb.Append(':');
                sb.Append(histogramMeasurement.BucketCount);
                previousExplicitBound = histogramMeasurement.ExplicitBound;
                isFirstIteration = false;
            }
            else
            {
                sb.Append('(');
                sb.Append(previousExplicitBound);
                sb.Append(',');
                if (!double.IsPositiveInfinity(histogramMeasurement.ExplicitBound))
                {
                    sb.Append(histogramMeasurement.ExplicitBound);
                    previousExplicitBound = histogramMeasurement.ExplicitBound;
                }
                else
                {
                    sb.Append("+Infinity");
                }

                sb.Append(']');
                sb.Append(':');
                sb.Append(histogramMeasurement.BucketCount);
            }
        }
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