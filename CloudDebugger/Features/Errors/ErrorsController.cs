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
            _logger.LogInformation("Responded with a 400 Bad Request status");
            return StatusCode(400, "Bad Request");
        }

        public IActionResult Error500()
        {
            _logger.LogInformation("Responded with a 500 Internal Server Error status");
            return StatusCode(500, "Internal Server Error");
        }

        public IActionResult Error503()
        {
            _logger.LogInformation("Responded with a 503 Service Unavailable status");
            return StatusCode(503, "Service Unavailable");
        }

        public IActionResult Exception()
        {
            _logger.LogInformation("Responded with a thrown InvalidOperationException exception");
            throw new InvalidOperationException("This is an InvalidOperationException!");
        }

        public IActionResult SlowPage()
        {
            _logger.LogInformation("The slow page was called (5 seconds delay)");
            // Block the thread
            Thread.Sleep(5000);

            return Content("Page is done!");
        }

    }
}
