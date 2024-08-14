using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.API
{
    /// <summary>
    /// Controller for the API HTML summary page
    /// </summary>
    public class APIController : Controller
    {
        private readonly ILogger<APIController> _logger;

        public APIController(ILogger<APIController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
