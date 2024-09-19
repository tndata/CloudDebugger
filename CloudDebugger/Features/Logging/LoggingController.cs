using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CloudDebugger.Features.Logging;

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
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        //TODO: Refactor!!
        model ??= new LoggingModel();

        return View("WriteToLog", model);
    }

    [HttpPost("/Logging/WriteToLog")]
    public IActionResult WriteToLogAction(LoggingModel model)
    {
        if (ModelState.IsValid && model != null)
        {
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
        // Define the regex pattern to allow only spaces, numerical, and alphabetical characters
        string pattern = @"[^a-zA-Z0-9\s]";
        // Replace all unwanted characters with an empty string
        string filteredMessage = Regex.Replace(message, pattern, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));

        _logger.Log(logLevel, "This message was written with the {LogLevel} log level. {Message}", logLevel, filteredMessage);
    }
}
