using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.QueueStorage;

/// <summary>
/// Azure Queue Storage main page
/// 
/// This is just for the main page. The actual tools are separate features in this project.
/// </summary>
public class QueueStorageController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
