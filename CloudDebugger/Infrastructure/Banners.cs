using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CloudDebugger.Infrastructure;


/// <summary>
/// Startup banners
/// 
/// We have two different banners, one before ASP.NET Core configuration and one after it has started. We do this, so that the first banner is always displayed even if the startup fails.
/// 
/// Reference:
/// * https://weblog.west-wind.com/posts/2024/Sep/03/Getting-the-ASPNET-Core-Server-Hosting-Urls-during-Server-Startup#which-port-am-i-hosting
/// * https://weblog.west-wind.com/posts/2021/Nov/09/Add-an-ASPNET-Runtime-Information-Startup-Banner
/// </summary>
public static class Banners
{
    public static void DisplayPreStartupBanner()
    {
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
        Log.Information("Project Build time: {StartTime} UTC", versionInfo?.LegalCopyright);
    }


    public static void DisplayPostStartupBanner(WebApplication app, WebApplicationBuilder builder)
    {
        Console.WriteLine("========================================================="); //We don't wan this in the log

        var urlList = app.Urls;
        string urls = string.Join(" ", urlList);

        Log.Information("CloudDebugger started, listening to: {Urls}", urls);
        Console.ResetColor();

        Log.Information("Runtime: {FrameworkDescription} - {EnvironmentName}, Platform: {OSDescription} ({OSArchitecture})",
                            RuntimeInformation.FrameworkDescription,
                            builder.Environment.EnvironmentName,
                            RuntimeInformation.OSDescription,
                            RuntimeInformation.OSArchitecture);

        Console.WriteLine("=========================================================");
    }
}
