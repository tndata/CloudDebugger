using Azure.Core.Diagnostics;
using Azure.MyIdentity;
using CloudDebugger;
using CloudDebugger.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;

Console.Title = "CloudDebugger";
Settings.StartupTime = DateTime.UtcNow;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Session", LogEventLevel.Verbose)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();


//Print the logo to make it easier to see when this app starts up in the logs
Log.Information("CloudDebugger starting up");
Log.Information("");
Log.Information(@"  _________ .__                   .___________        ___.                                      ");
Log.Information(@"  \_   ___ \|  |   ____  __ __  __| _/\______ \   ____\_ |__  __ __  ____   ____   ___________  ");
Log.Information(@"  /    \  \/|  |  /  _ \|  |  \/ __ |  |    |  \_/ __ \| __ \|  |  \/ ___\ / ___\_/ __ \_  __ \ ");
Log.Information(@"  \     \___|  |_(  <_> )  |  / /_/ |  |    `   \  ___/| \_\ \  |  / /_/  > /_/  >  ___/|  | \/ ");
Log.Information(@"   \______  /____/\____/|____/\____ | /_______  /\___  >___  /____/\___  /\___  / \___  >__|    ");
Log.Information(@"          \/                       \/         \/     \/    \/     /_____//_____/      \/        ");
Log.Information("");

//Get the build date, it is set in the project file, see https://stackoverflow.com/a/50607951/68490
var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location ?? "");
Log.Information("Project Build time: {StartTime}", versionInfo?.LegalCopyright);


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

    await app.RunAsync();

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
