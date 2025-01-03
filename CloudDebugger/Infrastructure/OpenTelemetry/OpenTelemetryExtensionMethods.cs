using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace CloudDebugger.Infrastructure.OpenTelemetry;

/// <summary>
/// Extension methods to configure OpenTelemetry
/// 
/// It supports two modes
/// * Default
///   - Logs
///     - Sent to Console and AddInMemoryExporter
///   - Metrics
///     - Sent to AddInMemoryExporter
///   - Traces
///     - Sent to AddInMemoryExporter
///     
/// * ApplicationInsights
///   - Logs
///   - Metrics
///   - Traces
///     All sent to ApplicationInsights
///     
/// ApplicationInsights is enabled by providing the connection string
/// </summary>
public static class OpenTelemetryExtensionMethods
{
    /// <summary>
    /// Configure application insights
    /// 
    /// https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?
    /// 
    /// GitHub: Azure Monitor Distro client library for .NET
    /// https://github.com/Azure/azure-sdk-for-net/tree/Azure.Monitor.OpenTelemetry.AspNetCore_1.2.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        //Configure OpenTelemetry with the default setup
        ConfigurDefaultOpenTelemetry(builder);

        var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionString))
        {
            ConfigureApplicationInsights(builder, connectionString);
        }
    }


    /// <summary>
    /// Getting Started with Jaeger
    /// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/getting-started-jaeger
    /// OTLP Exporter for OpenTelemetry .NET
    /// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md
    /// </summary>
    /// <param name="builder"></param>
    private static void ConfigurDefaultOpenTelemetry(WebApplicationBuilder builder)
    {
        Log.Information("Configuring and using Default OpenTelemetrySupport (Console and in-memory exporter)");

        // OTEL Server must be in this form for Jaeger: http://[domain]:4318/v1/traces
        var otlpServer = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "";

        ICollection<Activity> exportedTraceItems = OpenTelemetryObserver.TraceItemsLog;
        ICollection<Metric> exportedMetricItems = OpenTelemetryObserver.MetricItemsLog;
        ICollection<LogRecord> exportedLogItems = OpenTelemetryObserver.LogItemsLog;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CloudDebugger"))
            .WithLogging(logging =>
            {
                logging.AddProcessor(new SimpleLogRecordExportProcessor(new CustomConsoleExporter()));
                logging.AddInMemoryExporter(exportedLogItems);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddInMemoryExporter(exportedMetricItems, o =>
                       {
                           o.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                       }).AddConsoleExporter()
                       .AddPrometheusExporter();

                //Custom Histogram with fixed custom bucket sizes
                metrics.AddView("AdvancedHistogram", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000]
                });

                metrics.AddMeter("CloudDebugger.Counters");
                GlobalSettings.PrometheusExporterEnabled = true;
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddInMemoryExporter(exportedTraceItems);

                // Add the OTLP Exporter if the OTEL_EXPORTER_OTLP_ENDPOINT variable is set
                if (!string.IsNullOrEmpty(otlpServer))
                {
                    otlpServer = otlpServer.Trim();
                    Log.Information("Enabling OTLP Exporter to '{server}'", otlpServer);

                    tracing.AddOtlpExporter(o =>
                    {
                        o.ExportProcessorType = ExportProcessorType.Simple;
                        o.Endpoint = new Uri(otlpServer);
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }

                tracing.AddSource("CloudDebugger*");
                tracing.AddSource("Microsoft.AspNetCore*");
            });

        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) =>
            {
                return true;    //Include everything
            };
        });

        builder.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
        {
            options.RecordException = true;
            options.FilterHttpRequestMessage = (httpRequestMessage) =>
            {
                return true;    //Include everything
            };
        });

        builder.Logging.AddFilter("Default", LogLevel.Debug);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("CloudDebugger", LogLevel.Trace);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore", LogLevel.Information);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Information);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
    }


    /// <summary>
    /// Important, UseMyAzureMonitor will crash with an exception if the connection string is missing :-(
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="connectionString"></param>
    private static void ConfigureApplicationInsights(WebApplicationBuilder builder, string? connectionString)
    {
        // Default mode

        Log.Information("Application Insights enabled.");
        Log.Information($"ConnectionString: {connectionString}");

        builder.Services.AddOpenTelemetry()
                        .UseAzureMonitor(o =>
                        {
                            o.ConnectionString = connectionString;

                            o.Diagnostics.IsDistributedTracingEnabled = true;
                            o.Diagnostics.IsLoggingEnabled = true;
                            o.Diagnostics.IsTelemetryEnabled = true;

                            o.EnableLiveMetrics = true;
                            o.SamplingRatio = 1.0f;         //The default value is 1.0F, indicating that all telemetry items are sampled.
                        });


        builder.Logging.AddFilter("Default", LogLevel.Debug);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("CloudDebugger", LogLevel.Trace);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore", LogLevel.Information);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Information);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
    }
}
