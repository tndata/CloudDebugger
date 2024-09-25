using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CloudDebugger.Features.Logging;

/// <summary>
/// This tool will write a message to each of the 6 different log levels
/// </summary>
public class LoggingController : Controller
{
    private readonly ILoggerFactory loggerFactory;

    public LoggingController(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public IActionResult Index()
    {
        var model = new LoggingModel()
        {
            LogMessage = "This is my log message!",
            LogCategory = typeof(LoggingController).FullName
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Index(LoggingModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                string message = model.LogMessage ?? "This is my log message!";

                WriteToLog(message, model.LogCategory, LogLevel.Trace);
                WriteToLog(message, model.LogCategory, LogLevel.Debug);
                WriteToLog(message, model.LogCategory, LogLevel.Information);
                WriteToLog(message, model.LogCategory, LogLevel.Warning);
                WriteToLog(message, model.LogCategory, LogLevel.Error);
                WriteToLog(message, model.LogCategory, LogLevel.Critical);

                model.Message = "Log messages written successfully!";
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }

    private void WriteToLog(string message, string? logCategory, LogLevel logLevel)
    {
        // Define the regex pattern to allow only spaces, numerical, and alphabetical characters
        string pattern = @"[^a-zA-Z0-9\s]";
        // Replace all unwanted characters with an empty string
        string filteredMessage = Regex.Replace(message, pattern, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));

        var log = loggerFactory.CreateLogger(logCategory ?? typeof(LoggingController).FullName!);
        log.Log(logLevel, "This message was written with the {LogLevel} log level. {Message}", logLevel, filteredMessage);
    }
}
