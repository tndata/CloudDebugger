using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.RequestSender;

public class RequestSenderModel
{
    /// <summary>
    /// HTTP method for the request (e.g., GET, POST, PUT, DELETE)
    /// </summary>
    [Required]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// URL to which the request is sent
    /// </summary>
    [Required]
    public string? URL { get; set; }

    /// <summary>
    /// Headers to include in the request
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// Optional authentication token
    /// </summary>
    public string? AuthenticationToken { get; set; }

    /// <summary>
    /// Body content of the request
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Headers received in the response
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// Body content of the response
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Status codes received in the response
    /// </summary>
    public string? ResponseStatusCode { get; set; }

    /// <summary>
    /// Error message if the request fails
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// General message about the operation
    /// </summary>
    public string? Message { get; set; }
}
