using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventHub;

public class SendEventHubModel
{
    [Required]
    public string? ConnectionString { get; set; } = "";
    [Required]
    public int StartNumber { get; set; } = 1;
    [Required]
    public int NumberOfEvents { get; set; } = 10;

    public string? Exception { get; set; }

    public string? Message { get; set; }
}