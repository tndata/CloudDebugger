using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.ScaleOut;

public class ScaleOutController : Controller
{
    private readonly ILogger<ScaleOutController> _logger;

    public ScaleOutController(ILogger<ScaleOutController> logger)
    {
        _logger = logger;
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

        if (!string.IsNullOrEmpty(clearcookies))
        {
            Response.Headers["Clear-Site-Data"] = "\"cookies\"";
        }

        // We don't want browser caching
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate, proxy-revalidate, private  ";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";


        return View(model);
    }
}
