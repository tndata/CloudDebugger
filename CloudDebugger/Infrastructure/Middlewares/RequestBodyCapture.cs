using System.Text;

namespace CloudDebugger.Infrastructure.Middlewares;

public static class RequestBodyCaptureExtensions
{
    /// <summary>
    /// This middleware is used to capture the request body. It is used by the Request Logger tool.
    /// </summary>
    /// <param name="app"></param>
    public static IApplicationBuilder UseRequestBodyCapture(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestBodyCaptureMiddleware>();
    }
}


public class RequestBodyCaptureMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate next;

    public RequestBodyCaptureMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<RequestBodyCaptureMiddleware>();
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        _logger.LogDebug("RequestBodyCaptureMiddleware was called");

        await CaptureBody(context);

        // Call the next middleware in the pipeline
        await next(context);
    }

    private static async Task CaptureBody(HttpContext context)
    {
        try
        {
            //Try to read the request body (for the Request Logger). The data is then consumed inside the MyHttpLogging middleware
            string bodyStr = "";
            // Arguments: Stream, Encoding, detect encoding, buffer size 
            // AND, the most important: keep stream opened
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyStr = await reader.ReadToEndAsync();

                context.Items.Add("RequestBody", bodyStr);
            }
        }
        catch (Exception)
        {
            // Safe to ignore: body capture is optional diagnostic functionality.
            // If it fails (e.g., stream already consumed, encoding issues), the request
            // should still proceed normally. The Request Logger tool simply won't show the body.
        }

        context.Request.Body.Position = 0;
    }
}
