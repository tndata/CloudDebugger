using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.LogWorkspace;

/// <summary>
/// Send log data to Log Analytics with the HTTP Data Collector API
/// https://learn.microsoft.com/en-us/rest/api/loganalytics/create-request
/// </summary>
public class LogWorkspaceController : Controller
{
    private readonly ILogger<LogWorkspaceController> _logger;

    private const string workspaceId = "LogAnalyticsWorkspaceId";
    private const string workspaceKey = "LogAnalyticsWorkspaceKey";
    private const int NumberOfLogStatementsToSend = 10;

    public LogWorkspaceController(ILogger<LogWorkspaceController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new LogWorkspaceModel()
        {
            LogMessage = "This is my custom message",
            LogType = "MyApplicationLog",
            WorkspaceId = HttpContext.Session.GetString(workspaceId),
            WorkspaceKey = HttpContext.Session.GetString(workspaceKey)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(LogWorkspaceModel model)
    {
        if (ModelState.IsValid && model != null)
        {
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(model.LogType);
                ArgumentNullException.ThrowIfNullOrEmpty(model.WorkspaceId);
                ArgumentNullException.ThrowIfNullOrEmpty(model.WorkspaceKey);

                model.Exception = "";
                model.Message = "";

                //Remember access ID and key
                HttpContext.Session.SetString(workspaceId, model.WorkspaceId);
                HttpContext.Session.SetString(workspaceKey, model.WorkspaceKey);

                var logStatements = GenerateSampleLogStatements(model.LogMessage);

                await LogSender.SendLogStatements(logStatements, model.LogType!, model.WorkspaceId!, model.WorkspaceKey!);

                model.Message = $"Log statements sent to table {model.LogType} in your Log Analytics Workspace! It will take a few minutes before the data shows up in your Workspace.";

                _logger.LogInformation("Send log statements to Log Analytics Workspace.");
            }
            catch (Exception exc)
            {
                string msg = "";
                model.Exception = msg + exc.ToString();
            }
        }

        return View(model);
    }

    private static List<LogWorkspaceLogEntry> GenerateSampleLogStatements(string? logMessage)
    {
        var tmp = new List<LogWorkspaceLogEntry>();

        var severity = new List<string>() { "Information", "Warning", "Error" };
        var random = new Random();

        for (int i = 0; i < NumberOfLogStatementsToSend; i++)
        {
            //Select a random severity  
            var index = random.Next(severity.Count);
            var logData = new LogWorkspaceLogEntry()
            {
                Message = logMessage + " #" + i,
                Severity = severity[index],
                Timestamp = DateTime.UtcNow
            };

            tmp.Add(logData);
        }

        return tmp;
    }
}
