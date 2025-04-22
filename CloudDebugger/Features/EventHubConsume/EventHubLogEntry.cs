namespace CloudDebugger.Features.EventHubConsume;

public class EventHubLogEntry
{
    public List<string>? EventDetails { get; set; }

    public string? Offset { get; set; }

    public string? PartitionId { get; set; }

    public string? Body { get; set; }
}