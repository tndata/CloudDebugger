using CloudDebugger.Infrastructure;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Diagnostics;

namespace CloudDebugger.Infrastructure.OpenTelemetry;

public static class OpenTelemetryObserver
{
    public static ICollection<Activity> TraceItemsLog { get; } = new BoundedConcurrentQueue<Activity>(50);    //Keep the last 50 entries
    public static ICollection<Metric> MetricItemsLog { get; } = new BoundedConcurrentQueue<Metric>(50);
    public static ICollection<LogRecord> LogItemsLog { get; } = new BoundedConcurrentQueue<LogRecord>(50);
}
