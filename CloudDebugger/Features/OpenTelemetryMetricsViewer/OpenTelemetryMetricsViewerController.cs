using CloudDebugger.Infrastructure.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using System.Globalization;
using System.Text;

namespace CloudDebugger.Features.OpenTelemetryMetricsViewer;

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
public class OpenTelemetryMetricsViewerController : Controller
{
    public OpenTelemetryMetricsViewerController()
    {
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

        var deduplicatedMetrics = Deduplicate(metricItemsLog);

        foreach (Metric metric in deduplicatedMetrics.Values)
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
                sb.AppendLine($"  --Point--");
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
                    sb.AppendLine($"    {pointTag.Key}: {pointTag.Value}");
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


    private static Dictionary<string, Metric> Deduplicate(ICollection<Metric> metricItemsLog)
    {
        var deduplicatedMetrics = new Dictionary<string, Metric>();

        foreach (var metric in metricItemsLog)
        {
            // Build a unique key based on metric name and labels
            var keyBuilder = new StringBuilder(metric.Name);

            foreach (ref readonly var point in metric.GetMetricPoints())
            {
                foreach (var tag in point.Tags)
                {
                    keyBuilder.Append($"|{tag.Key}:{tag.Value}");
                }
            }

            var key = keyBuilder.ToString();

            // Store the metric, overwriting older ones with the same key
            deduplicatedMetrics[key] = metric;
        }

        return deduplicatedMetrics;
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
        sb.AppendLine();
        sb.AppendLine("== Buckets ==");

        bool isFirstIteration = true;
        double previousExplicitBound = default;
        foreach (var histogramMeasurement in point.GetHistogramBuckets())
        {
            if (isFirstIteration)
            {
                sb.Append("  (-Infinity,");
                sb.Append(histogramMeasurement.ExplicitBound);
                sb.Append(']');
                sb.Append(':');
                sb.Append(histogramMeasurement.BucketCount);
                sb.AppendLine();

                previousExplicitBound = histogramMeasurement.ExplicitBound;
                isFirstIteration = false;
            }
            else
            {
                sb.Append("  [");
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
                sb.AppendLine();
            }
        }
    }
}