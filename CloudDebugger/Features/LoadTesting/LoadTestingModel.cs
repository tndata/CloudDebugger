using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.LoadTesting;

public class LoadTestingModel
{
    [Required]
    /// <summary>
    /// Gets or sets the total number of requests to be sent during the load test.
    /// </summary>
    public int TotalNumberOfRequests { get; set; }

    [Required]
    /// <summary>
    /// Gets or sets the number of concurrent requests to be sent during the load test.
    /// </summary>
    public int NumberOfConcurrentRequests { get; set; }

    [Required]
    /// <summary>
    /// Gets or sets the target URL for the load test.
    /// </summary>
    public string? TargetURL { get; set; }

    /// <summary>
    /// Gets or sets the exception message if an error occurs during the load test.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets a message related to the load test.
    /// </summary>
    public string? Message { get; set; }
}
