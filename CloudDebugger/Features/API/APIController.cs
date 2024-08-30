using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Api
{
    /// <summary>
    /// Controller for the API HTML summary page
    /// </summary>
    public class ApiController : Controller
    {
        public ApiController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
