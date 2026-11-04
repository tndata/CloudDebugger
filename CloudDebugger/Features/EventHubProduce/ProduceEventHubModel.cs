using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventHubProduce;

public class ProduceEventHubModel
{
    [Required]
    public string? ConnectionString { get; set; } = "";
    [Required]
    public int StartNumber { get; set; } = 1;
    [Range(1, int.MaxValue, ErrorMessage = "Enter a number of events greater than zero.")]
    public int NumberOfEvents { get; set; } = 10;

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}