using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace CloudDebugger.Features.Diagnostics;

/// <summary>
/// 
/// </summary>
public class DiagnosticsController : Controller
{
    public DiagnosticsController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult EnvironmentVariables()
    {
        return View();
    }

    public IActionResult Network()
    {
        //TODO: FIX!!
        //Inspiration https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface?view=net-8.0&redirectedfrom=MSDN

        // More code here https://github.com/dotnet/dotnet-api-docs/blob/a4cef208decae4c2863337173050b2805ec8f706/snippets/csharp/System.Net.NetworkInformation/IcmpV4Statistics/Overview/netinfo.cs#L316
        return View();
    }


    /// <summary>
    /// Display various runtime information
    /// 
    /// Resources:
    /// https://weblog.west-wind.com/posts/2024/Sep/03/Getting-the-ASPNET-Core-Server-Hosting-Urls-during-Server-Startup
    /// </summary>
    /// <param name="server"></param>
    /// <returns></returns>
    public IActionResult SystemRuntimeDetails([FromServices] IServer server)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var model = new SystemRuntimeDetailsModel
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeinformation
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            OSDescription = RuntimeInformation.OSDescription,
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,

            //https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeenvironmen
            RuntimeDirectory = RuntimeEnvironment.GetRuntimeDirectory(),
            SystemVersion = RuntimeEnvironment.GetSystemVersion(),

            RunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "No",

            ServerAddresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses?.ToList() ?? []
        };

        return View(model);
    }
}
