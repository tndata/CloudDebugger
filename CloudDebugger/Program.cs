using Azure.Core.Diagnostics;
using CloudDebugger;
using CloudDebugger.Infrastructure;
using CloudDebugger.Infrastructure.OpenTelemetry;
using CloudDebugger.SharedCode.AzureSdkEventLogger;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Diagnostics.Tracing;

Console.Title = "CloudDebugger";
DebuggerSettings.StartupTime = DateTime.UtcNow;

// Two-phase logging: Bootstrap logger handles logging BEFORE the WebApplication is built.
// Once the app starts, the main logging pipeline (configured in HostingExtensions) takes over.
// This ensures we can log startup errors even if the app fails to initialize.
var bootstrapLoggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddOpenTelemetry(logging =>
    {
        logging.SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService("CloudDebugger.BootstrapLogger"));
        logging.AddProcessor(new SimpleLogRecordExportProcessor(new CustomConsoleExporter()));
    });
});

Log.Configure(bootstrapLoggerFactory);

// Log all internal Azure SDK events. These events can then be viewed by the AzureSDKEventViewer tool.
var eventListener = new AzureEventSourceListener((evnt, message) =>
{
    AzureEventLogger.AddEventToLog(evnt);
},
level: EventLevel.Verbose);

try
{
    Banners.DisplayPreStartupBanner();

    // CreateSlimBuilder (vs CreateBuilder) excludes default middleware and services,
    // giving us full control over what's included in the request pipeline.
    var builder = WebApplication.CreateSlimBuilder(args);

    var app = builder
      .ConfigureServices(args)
      .ConfigurePipeline();

    await app.StartAsync();

    Banners.DisplayPostStartupBanner(app, builder);

    await app.WaitForShutdownAsync();

}
catch (Exception ex)
{
    Log.Critical(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutdown complete");
    eventListener.Dispose();
    bootstrapLoggerFactory.Dispose();
}


