using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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
            SiteName = Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME"),
            HostName = Environment.GetEnvironmentVariable("HOSTNAME"),

            PodIP = Environment.GetEnvironmentVariable("MY_POD_IP"),
            IPAddress = Environment.GetEnvironmentVariable("WEBSITE_INFRASTRUCTURE_IP"),
            InstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
            ComputerName = Environment.GetEnvironmentVariable("COMPUTERNAME"),

            CurrentTime = DateTime.UtcNow.ToString("HH:mm:ss"),
            RunningTime = DateTime.UtcNow.Subtract(DebuggerSettings.StartupTime)
        };

        //Calculate a HTML color based on the hash of the model
        //So we can have a unique color per instance of this application    
        var strHash = model.GetHashCode();
        var hash = strHash % 255 * 255 * 255;
        var hexColor = hash.ToString("X");
        model.BackgroundColor = hexColor;

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
}
