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
            //TODO: Refactor!!
            model ??= new LoggingModel();

            return View("WriteToLog", model);
        }

        [HttpPost("/Logging/WriteToLog")]
        public IActionResult WriteToLogAction(LoggingModel model)
        {
            if (ModelState.IsValid)
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
            }

            return View("WriteToLog", model);
        }

        private void WriteToLog(string message, LogLevel logLevel)
        {
            _logger.Log(logLevel, "This message was written with the {LogLevel} log level. {Message}", logLevel, message);
        }
    }
}
