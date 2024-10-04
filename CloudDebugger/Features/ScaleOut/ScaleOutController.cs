using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CloudDebugger.Features.ScaleOut;

/// <summary>
/// This tool is useful for testing the scale out capabilities of your application by calling the application from within four iframes on the same page.
/// </summary>
public class ScaleOutController : Controller
{
    public ScaleOutController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult MultiInstances()
    {
        return View();
    }

    public IActionResult MultiInstancesNoCookies()
    {
        return View();
    }

    /// <summary>
    /// Show the details about the current service instance
    /// </summary>
    /// <param name="clearcookies"></param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult ShowDetails(string clearcookies = "")
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var model = new ScaleOutModel()
        {
            SiteName = TryGetSiteName(),
            HostName = TryGetHostName(),
            PodIP = Environment.GetEnvironmentVariable("MY_POD_IP"),
            IPAddress = TryGetLocalIPAddress(),
            InstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
            ComputerName = Environment.GetEnvironmentVariable("COMPUTERNAME"),

            CurrentTime = DateTime.UtcNow.ToString("HH:mm:ss"),
            RunningTime = DateTime.UtcNow.Subtract(DebuggerSettings.StartupTime)
        };

        model.BackgroundColor = CalculateInstanceColor(model);

        // If the clearcookies parameter is set, we will send a Clear-Site-Data header to the browser
        if (!string.IsNullOrEmpty(clearcookies))
        {
            Response.Headers["Clear-Site-Data"] = "\"cookies\"";
        }

        // We don't want browser caching on this page.
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate, proxy-revalidate, private  ";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return View(model);
    }

    private static string CalculateInstanceColor(ScaleOutModel model)
    {
        //Calculate a HTML color based on the hash of the model
        //So we can have a unique color per instance of this application    
        var strHash = model.GetHashCode();
        var hash = strHash % 0xFFFFFF;
        var hexColor = hash.ToString("X6"); // Format as a 6-digit hex value
        return hexColor;
    }

    private static string? TryGetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                                              !IPAddress.IsLoopback(ip));

        if (ipAddress != null)
        {
            return ipAddress.ToString();
        }
        else
        {
            //Try this one as a backup
            return Environment.GetEnvironmentVariable("WEBSITE_INFRASTRUCTURE_IP");
        }
    }

    private static string? TryGetHostName()
    {
        var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
        if (string.IsNullOrEmpty(hostName))
        {
            //Container Apps specific instance indentifer
            hostName = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
        }
        return hostName;
    }

    private static string? TryGetSiteName()
    {
        var siteName = Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME");

        if (string.IsNullOrEmpty(siteName))
        {
            //Container Apps specific instance name
            siteName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME");
        }
        return siteName;
    }
}
