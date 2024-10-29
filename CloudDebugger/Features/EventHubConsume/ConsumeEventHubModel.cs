using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventHubConsume;

public class ConsumeEventHubModel
{
    [Required]
    public string? ConnectionString { get; set; } = "";

    [Required]
    public string? ConsumerGroup { get; set; } = "$Default";

    public List<EventHubLogEntry> Events { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

}