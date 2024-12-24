using Azure.Core.Diagnostics;
using CloudDebugger;
using CloudDebugger.Infrastructure;
using CloudDebugger.SharedCode.AzureSDKEventLogger;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Tracing;

Console.Title = "CloudDebugger";
DebuggerSettings.StartupTime = DateTime.UtcNow;


//Startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Session", LogEventLevel.Verbose)
    .Enrich.FromLogContext()
    // .WriteTo.Console()
    .CreateBootstrapLogger();


Banners.DisplayPreStartupBanner();


// Log all internal Azure SDK events. These events can then be viewed by the AzureSDKEventViewer tool.
var eventListener = new AzureEventSourceListener((evnt, message) =>
{
    AzureEventLogger.AddEventToLog(evnt, message);
},
level: EventLevel.Verbose);


try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, logger) =>
    {
        logger
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .Enrich.FromLogContext();
    });

    var app = builder
      .ConfigureServices()
      .ConfigurePipeline();

    await app.StartAsync();

    Banners.DisplayPostStartupBanner(app, builder);

    await app.WaitForShutdownAsync();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    eventListener.Dispose();
    Log.Information("Shut down complete");
    await Log.CloseAndFlushAsync();
}


