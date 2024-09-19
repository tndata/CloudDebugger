using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.LogWorkspace;

/// <summary>
/// Send log data to Log Analytics with the HTTP Data Collector API
/// https://learn.microsoft.com/en-us/rest/api/loganalytics/create-request
/// </summary>
public class LogWorkspaceController : Controller
{
    private readonly ILogger<LogWorkspaceController> _logger;

    //"remember" the workspaceId and workspaceKey

    //TODO: Use session here
    private static string workspaceId;
    private static string workspaceKey;

    // The name of the table to write to in Log Analytics workspace
    // We keep it hardcoded for now. The table will be automatically created if not found. 
    private const string logType = "MyApplicationLog";

    public LogWorkspaceController(ILogger<LogWorkspaceController> logger)
    {
        _logger = logger;

        workspaceId = "";
        workspaceKey = "";
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/LogWorkspace/SendEvents")]
    public IActionResult GetSendEvents(LogWorkspaceModel model)
    {
        _logger.LogInformation("LogWorkspace.GetSendEvents was called");
        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        //TODO: Refactor!!
        model ??= new LogWorkspaceModel();

        model.WorkspaceId = workspaceId;
        model.WorkspaceKey = workspaceKey;
        model.LogType = logType;
        ModelState.Clear();

        return View("SendEvents", model);
    }

    [HttpPost("/LogWorkspace/SendEvents")]
    public async Task<IActionResult> PostSendEvents(LogWorkspaceModel model)
    {
        _logger.LogInformation("LogWorkspace.PostSendEvents was called");

        if (ModelState.IsValid && model != null && model.WorkspaceId != null && model.WorkspaceKey != null)
        {
            try
            {
                model.Exception = "";
                model.Message = "";

                var logStatements = GenerateLogStatements(model.LogMessage);

                await SendLogStatements(logStatements, logType, model.WorkspaceId, model.WorkspaceKey);

                model.Message = $"Log statements sent to table {logType} in your Log Analytics Workspace! It will take a few minutes before the data shows up in the table.";
            }
            catch (Exception exc)
            {
                string msg = "";
                model.Exception = msg + exc.ToString();
            }
        }

        return View("SendEvents", model);
    }

    private static Task SendLogStatements(List<LogWorkspaceLogEntry> logStatements, string logType, string workspaceId, string workspaceKey)
    {
        return LogSender.SendLogStatements(logStatements, logType, workspaceId, workspaceKey);
    }

    private static List<LogWorkspaceLogEntry> GenerateLogStatements(string? logMessage)
    {
        var tmp = new List<LogWorkspaceLogEntry>();

        var severity = new List<string>() { "Information", "Warning", "Error" };

        for (int i = 0; i < 10; i++)
        {
            //Select a random severity  
            var random = new Random();
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
