using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventHub;

/// <summary>
/// Event Hub main page
/// 
/// This is just for the main page. The actual tools are separate features in this project.
/// </summary>
public class EventHubController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
