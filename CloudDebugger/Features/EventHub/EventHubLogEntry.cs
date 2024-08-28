namespace CloudDebugger.Features.EventHub;

public class EventHubLogEntry
{
    public List<string>? EventDetails { get; set; }

    public long Offset { get; set; }

    public string? PartitionId { get; set; }

    public DateTime EntryTime { get; set; }

    public string? Body { get; set; }
}