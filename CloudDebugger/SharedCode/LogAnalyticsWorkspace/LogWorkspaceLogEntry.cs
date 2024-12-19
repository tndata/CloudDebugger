namespace CloudDebugger.SharedCode.LogAnalyticsWorkspace;

/// <summary>
/// Represents a log entry in the log workspace.
/// 
/// Standard columns in Azure Monitor Logs
/// https://learn.microsoft.com/en-us/azure/azure-monitor/logs/log-standard-columns 
/// </summary>
public class LogWorkspaceLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp of when the log event occurred.
    /// </summary>
    public DateTime TimeGenerated { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the log entry.
    /// Supported severities: "Info", "Warning", "Error", "Critical"
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Gets or sets the name of the log type or table associated with the log entry.
    /// Example: "AuthenticationLog"
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID used to trace related logs across systems.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the Azure resource ID associated with the log entry.
    /// Example: "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{appName}"
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for the Azure tenant or workspace.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the log event, if applicable.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the operation being performed during the log event.
    /// Example: "UserLogin", "APIRequest"
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets or sets the client IP address associated with the log entry.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the name of the application that emitted the log.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the environment in which the log event occurred.
    /// Example: "Development", "Staging", "Production"
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the details of any exception associated with the log entry, if applicable.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets custom dimensions as key-value pairs for additional context in the log.
    /// Example: { "Module": "Authentication", "LoginMethod": "Password" }
    /// </summary>
    public Dictionary<string, string>? CustomDimensions { get; set; }

    /// <summary>
    /// Gets or sets numeric metrics or measurements related to the log entry.
    /// Example: { "ExecutionTimeMs": 150 }
    /// </summary>
    public Dictionary<string, double>? Metrics { get; set; }
}
