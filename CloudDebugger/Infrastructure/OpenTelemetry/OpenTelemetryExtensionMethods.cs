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
using System.Runtime.InteropServices;

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
        ConfigureDefaultOpenTelemetry(builder);

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
    private static void ConfigureDefaultOpenTelemetry(WebApplicationBuilder builder)
    {
        Log.Information("Configuring and using Default OpenTelemetrySupport (Console and in-memory exporter)");

        // OTEL Server must be in this form for Jaeger: http://[domain]:4318/v1/traces
        var otlpServer = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "";

        ICollection<Activity> exportedTraceItems = OpenTelemetryObserver.TraceItemsLog;
        ICollection<Metric> exportedMetricItems = OpenTelemetryObserver.MetricItemsLog;
        ICollection<LogRecord> exportedLogItems = OpenTelemetryObserver.LogItemsLog;

        var serviceName = GetServiceName();
        var instanceId = GetInstanceId();

        var serviceInstanceId = $"{instanceId}";
        if (!serviceInstanceId.Contains(serviceName))
        {
            //If the service name is not in the instance id, we will add it. To make it more human-readable.
            serviceInstanceId = $"{serviceName}-{instanceId}";
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceInstanceId: serviceInstanceId))
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
                })
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
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("System.Net", LogLevel.Debug);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("System.Net.Http.HttpClient", LogLevel.Information);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("CloudDebugger", LogLevel.Trace);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.Mvc", LogLevel.Warning);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Information);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Information);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Debug);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.HttpOverrides", LogLevel.Debug);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("CloudDebugger.Infrastructure.Middlewares", LogLevel.Warning);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("ModelContextProtocol", LogLevel.Debug);
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
    }


    /// <summary>
    /// Try to get the name of this service (App Service, Container Instance or Container App)
    /// We do this to try to improve the service name in Application Insights or Jaeger.
    /// </summary>
    /// <returns></returns>
    public static string GetServiceName()
    {
        //Running as App Service?
        var serviceName = Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME");
        if (serviceName is not null)
            return serviceName;

        //Running as Container Instance?
        var isACI = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" &&
                    Environment.GetEnvironmentVariable("Fabric_ApplicationName") != null;
        if (isACI)
        {
            return "Azure Container Instance";
        }

        // Running as a Container App?
        serviceName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME");
        if (serviceName is not null)
        {
            return serviceName; //For example 'ca-clouddebugger--6iru8hr-7f5846586f-s2v5h'
        }

        // Default service name 
        //Use a generic service name for OTEL
        serviceName = "CloudDebugger-Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            serviceName = "CloudDebugger-Windows";
        }

        return serviceName;
    }

    /// <summary>
    /// Try to get the name of this service Instance
    /// </summary>
    /// <returns></returns>
    public static string GetInstanceId()
    {
        // App Service
        var websiteId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        if (!string.IsNullOrEmpty(websiteId))
            return websiteId;

        // Container Apps
        var containerAppId = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
        if (!string.IsNullOrEmpty(containerAppId))
            return containerAppId;

        // Container Instances or fallback
        var machineName = Environment.MachineName;
        return machineName;
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
    }
}
