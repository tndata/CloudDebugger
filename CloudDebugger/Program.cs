using Azure.Core.Diagnostics;
using Azure.MyIdentity;
using CloudDebugger;
using CloudDebugger.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Tracing;

Console.Title = "CloudDebugger";
Settings.StartupTime = DateTime.UtcNow;


//Startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Session", LogEventLevel.Verbose)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();


Banners.DisplayPreStartupBanner();

// TODO: Cleanup
_ = new AzureEventSourceListener((e, message) =>
{
    // Only log messages from "Azure-Core" event source
    //Azure-Messaging-ServiceBus
    //Azure-Messaging-EventHubs
    //Azure-Messaging-ServiceBus

    if (e.EventSource.Name == "Azure-Core" || e.EventSource.Name == "Azure-Identity")
    {
        MyAzureIdentityLog.AddToLog(e.EventSource.Name.ToString(), message);
    }
    else
    {
        Console.WriteLine(e.EventSource.Name);
    }

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
    Log.Information("Shut down complete");
    await Log.CloseAndFlushAsync();
}


