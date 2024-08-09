using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventHub;

public class ConsumeEventHubModel
{
    [Required]
    public string? ConnectionString { get; set; } = "";

    [Required]
    public string? ConsumerGroup { get; set; } = "$Default";

    public string? Exception { get; set; }

    public string? Message { get; set; }

    public List<EventHubLogEntry> Events = new();
}