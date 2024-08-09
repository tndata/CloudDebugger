using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.HomePage
{
    public class ErrorsController : Controller
    {
        private readonly ILogger<ErrorsController> _logger;

        public ErrorsController(ILogger<ErrorsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Error400()
        {
            return StatusCode(400, "Bad Request");
        }

        public IActionResult Error500()
        {
            return StatusCode(500, "Internal Server Error");
        }

        public IActionResult Error503()
        {
            return StatusCode(503, "Service Unavailable");
        }

        public IActionResult Exception()
        {
            throw new Exception("This is an exception");
        }

        public IActionResult SlowPage()
        {
            // Block the thread
            Thread.Sleep(5000);

            return Content("Page is done!");
        }

    }
}
