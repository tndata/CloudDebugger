using CloudDebugger.SharedCode.LogAnalyticsWorkspace;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.LogWorkspaceDataGenerator;

/// <summary>
/// Send log data to Log Analytics with the HTTP Data Collector API
/// https://learn.microsoft.com/en-us/rest/api/loganalytics/create-request
/// </summary>
public class LogWorkspaceDataGeneratorController : Controller
{
    private readonly ILogger<LogWorkspaceDataGeneratorController> _logger;

    private const string workspaceId = "LogAnalyticsWorkspaceId";
    private const string workspaceKey = "LogAnalyticsWorkspaceKey";
    private const int NumberOfLogStatementsToSend = 100;

    public LogWorkspaceDataGeneratorController(ILogger<LogWorkspaceDataGeneratorController> logger)
    {
        _logger = logger;
    }

    public IActionResult SendCustomerData()
    {
        var model = new LogWorkspaceModel()
        {
            LogMessage = "This is my custom message",
            LogType = "MyCustomerData",
            WorkspaceId = HttpContext.Session.GetString(workspaceId),
            WorkspaceKey = HttpContext.Session.GetString(workspaceKey)
        };

        return View(model);
    }

    /// <summary>
    /// Send log entries to Log Analytics Workspace
    /// 
    /// the data is a sample customer events, that we can do simple queries against.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> SendCustomerData(LogWorkspaceModel model)
    {
        if (ModelState.IsValid && model != null)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(model.LogType);
                ArgumentException.ThrowIfNullOrEmpty(model.WorkspaceId);
                ArgumentException.ThrowIfNullOrEmpty(model.WorkspaceKey);

                model.Exception = "";
                model.Message = "";

                //Remember access ID and key
                HttpContext.Session.SetString(workspaceId, model.WorkspaceId);
                HttpContext.Session.SetString(workspaceKey, model.WorkspaceKey);

                var customerData = GenerateCustomerData();

                await LogAnalyticsWorkspaceSender.SendLogStatements(customerData, model.LogType!, model.WorkspaceId!, model.WorkspaceKey!);

                model.Message = $"{NumberOfLogStatementsToSend} Log statements sent to table {model.LogType} in your Log Analytics Workspace! It will take a few minutes before the data shows up in your Workspace.";

                _logger.LogInformation("Sent {NumberOfLogStatementsToSend} log statements to Log Analytics Workspace.", NumberOfLogStatementsToSend);
            }
            catch (Exception exc)
            {
                string msg = "";
                model.Exception = msg + exc.ToString();
            }
        }

        return View(model);
    }
    private static List<LogWorkspaceLogEntry> GenerateCustomerData()
    {
        var logEntries = new List<LogWorkspaceLogEntry>();

        // Static data for deterministic generation
        var severities = new List<string> { "Information", "Warning", "Error" };
        var environments = new List<string> { "Development", "Staging", "Production" };
        var customers = new List<string> { "Alice", "Bob", "Charlie", "Diana", "Eve" };
        var products = new List<(string Name, double Price)>
        {
            ("Laptop", 1200.99),
            ("Smartphone", 799.49),
            ("Headphones", 199.99),
            ("Smartwatch", 349.99),
            ("Tablet", 499.99)
        };


        for (int i = 0; i < NumberOfLogStatementsToSend; i++)
        {
            // Deterministically generate values using modulus
            var severity = severities[i % severities.Count];
            var environment = environments[i % environments.Count];
            var customer = customers[i % customers.Count];
            var (Name, Price) = products[i % products.Count];
            var quantity = i % 4 + 1; // Cycles between 1 and 4
            var totalAmount = Price * quantity;

            // Generate timestamp based on index
            var timestamp = DateTime.UtcNow.Date.AddMinutes(i % 1440); // Spreads entries across one day

            // Create log entry
            var logData = new LogWorkspaceLogEntry
            {
                TimeGenerated = timestamp,
                Message = $"{customer} purchased {quantity} x {Name} for ${totalAmount}.",
                Severity = severity,
                Type = "ECommerceLog",
                CorrelationId = $"CID-{i:D4}",
                ResourceId = "CloudDebugger",
                TenantId = "MyTenant",
                UserId = customer,
                OperationName = "Purchase",
                ClientIp = $"192.168.{i % 256}.{i * 7 % 256}", // Deterministic IP generation
                ApplicationName = "ECommercePlatform",
                Environment = environment,
                Exception = "",
                CustomDimensions = new Dictionary<string, string>
            {
                { "CustomerName", customer },
                { "ProductName", Name },
                { "PaymentStatus", severity == "Error" ? "Failed" : "Successful" }
            },
                Metrics = new Dictionary<string, double>
            {
                { "Quantity", quantity },
                { "UnitPrice", Price },
                { "TotalAmount", totalAmount }
            }
            };

            logEntries.Add(logData);
        }

        return logEntries;
    }
}
