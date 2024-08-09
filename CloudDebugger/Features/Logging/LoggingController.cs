using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Logging
{
    public class LoggingController : Controller
    {
        private readonly ILogger<LoggingController> _logger;

        public LoggingController(ILogger<LoggingController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/Logging/WriteToLog")]
        public IActionResult GetWriteToLogPage(LoggingModel model)
        {
            if (model == null)
            {
                model = new LoggingModel();
            }

            return View("WriteToLog", model);
        }

        [HttpPost("/Logging/WriteToLog")]
        public IActionResult WriteToLogAction(LoggingModel model)
        {
            model.LogMessage = "";

            string message = model.LogMessage ?? "This is my log message!";

            WriteToLog(message, LogLevel.Trace);
            WriteToLog(message, LogLevel.Debug);
            WriteToLog(message, LogLevel.Information);
            WriteToLog(message, LogLevel.Warning);
            WriteToLog(message, LogLevel.Error);
            WriteToLog(message, LogLevel.Critical);

            model.Message = "Log messages written successfully!";

            return View("WriteToLog", model);
        }

        private void WriteToLog(string message, LogLevel logLevel)
        {
            message = "This message was written with the " + logLevel + " log level.\r\n" + message + "\r\n";
            _logger.Log(logLevel, message);
        }
    }
}
