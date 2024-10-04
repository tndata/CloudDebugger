namespace CloudDebugger.Features.EventHubProduce.Events;

public class EventHubOrderEvent
{
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
}
