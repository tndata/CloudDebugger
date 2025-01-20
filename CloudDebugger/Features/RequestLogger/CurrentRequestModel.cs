using CloudDebugger.Infrastructure.Middlewares;

namespace CloudDebugger.Features.RequestLogger;

public class CurrentRequestModel
{
    /// <summary>
    /// Details about the incoming raw request, before its modified by any middleware
    /// </summary>
    public RawRequestDetails? RawRequest { get; set; }

    /// <summary>
    /// This is the final URL
    /// </summary>
    public string? FinalDisplayUrl { get; set; }
}
