using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.LogWorkspace;

public class LogWorkspaceModel
{
    [Required]
    public string? LogMessage { get; set; } = "This is my custom message";

    /// <summary>
    /// The name of the table to write to in Log Analytics workspace
    /// We keep it hardcoded for now. The table will be automatically crated 
    /// </summary>
    public string? LogType { get; set; }

    [Required]
    /// <summary>
    /// The ID of the Log Analytics workspace
    /// </summary>
    public string? WorkspaceId { get; set; }

    [Required]
    /// <summary>
    /// The Log Analytics workspace secret key
    /// </summary>
    public string? WorkspaceKey { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }
}
