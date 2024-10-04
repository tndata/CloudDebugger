using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventHub;

/// <summary>
/// Event Hub main page
/// </summary>
public class EventHubController : Controller
{
    public EventHubController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
