using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CloudDebugger.Features.Logging;

/// <summary>
/// This tool performs the following actions:
/// 
/// * Writes a message to standard output and standard error.
/// * Writes a message to the logging system at each of the 6 log levels: Trace, Debug, Information, Warning, Error, and Critical.
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
        return View();
    }

    public IActionResult WriteToStdOutErr()
    {
        var model = new StdOutStdErrModel()
        {
            StdOutMessage = "XXX - This is my standard output message! XXX",
            StdErrMessage = "XXX - This is my standard error message! XXX"
        };

        return View(model);
    }

    /// <summary>
    /// Write a message to the Standard output and Error output stream
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult WriteToStdOutErr(StdOutStdErrModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                if (model.StdOutMessage != null)
                    Console.WriteLine(model.StdOutMessage);

                if (model.StdErrMessage != null)
                    Console.Error.WriteLine(model.StdErrMessage);

                model.Message = "Log message written to standard out and/or error";
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }

    public IActionResult WriteToLog()
    {
        var model = new LoggingModel()
        {
            LogMessage = "XXX This is my log message! XXX",
            LogCategory = typeof(LoggingController).FullName
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult WriteToLog(LoggingModel model)
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
