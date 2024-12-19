using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.LogWorkspaceDataGenerator;

public class LogWorkspaceModel
{
    [Required]
    public string? LogMessage { get; set; }

    [Required]
    /// <summary>
    /// The name of the table to write to in Log Analytics workspace
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
