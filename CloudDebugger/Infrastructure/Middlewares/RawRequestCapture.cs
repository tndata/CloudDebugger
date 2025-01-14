namespace CloudDebugger.Infrastructure.Middlewares;

public static class RawRequestCaptureExtensions
{
    /// <summary>
    /// This middleware is used to capture the raw request details, before other middlewares modifies them. It is used by the Request Logger -> CurrentRequest tool.
    /// </summary>
    /// <param name="app"></param>
    public static IApplicationBuilder UseRawRequestCapture(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RawRequestCaptureMiddleware>();
    }
}


/// <summary>
/// Captures the raw request information (scheme, host, path, query, and RawTarget)
/// and stores it in HttpContext.Items[RawRequestDetailsKey].
/// </summary>
public class RawRequestCaptureMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate next;
    private const string RawRequestDetailsKey = "RawRequestDetails";


    public RawRequestCaptureMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<RawRequestCaptureMiddleware>();
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        CaptureDetails(context);

        // Call the next middleware in the pipeline
        await next(context);
    }

    private void CaptureDetails(HttpContext context)
    {
        // Reconstruct URL as it appears before ForwardedHeaders middleware
        var rawUrl = $"{context.Request.Scheme}://{context.Request.Host}" +
                     $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

        var remoteIP = context.Connection.RemoteIpAddress?.ToString() ?? "[unknown]";

        _logger.LogDebug("######################################################");
        _logger.LogDebug("rawUrl: {RawUrl}", rawUrl);
        _logger.LogDebug("RemoteIP: {RemoteIP}", remoteIP);

        // Capture all request headers
        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        foreach (var header in headers)
        {
            _logger.LogDebug("Raw header: {Header}: {Value}", header.Key, header.Value);
        }

        // Store these in HttpContext.Items so they can be retrieved later
        context.Items[RawRequestDetailsKey] = new RawRequestDetails
        {
            RawUrl = rawUrl,
            RemoteIpAddress = remoteIP,
            RawIncomingHeaders = headers
        };
    }
}


/// <summary>
/// A simple model to hold the raw request details.
/// </summary>
internal class RawRequestDetails
{
    /// <summary>
    ///  “friendly” reconstruction of the entire URL, including scheme and host
    /// </summary>
    public string? RawUrl { get; set; }

    /// <summary>
    ///  The client IP address
    /// </summary>
    public string? RemoteIpAddress { get; set; }

    /// <summary>
    /// All request headers as received by the application and before ForwardedHeaders makes any modifications.
    /// </summary>
    public Dictionary<string, string>? RawIncomingHeaders { get; set; }
}

