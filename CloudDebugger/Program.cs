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

// Step 2: Configure the global Log for bootstrap logging
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

    var builder = WebApplication.CreateBuilder(args);

    var app = builder
      .ConfigureServices()
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


