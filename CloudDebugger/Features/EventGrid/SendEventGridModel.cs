using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventGrid;

public class SendEventGridModel
{
    /// <summary>
    /// The topic to send to
    /// </summary>
    [Required]
    public string? TopicEndpoint { get; set; } = "";

    /// <summary>
    /// The starting event number
    /// </summary>
    [Required]
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// The number of events to send
    /// </summary>
    [Required]
    public int NumberOfEvents { get; set; } = 10;

    /// <summary>
    /// EventGrid access token (optional), if not provided, it will authenticate using DefaultAzureCredential (managed identity, environment variables, etc.)
    /// </summary>
    public string? AccessKey { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
