using System.Text;

namespace CloudDebugger.Infrastructure;

public static class CustomMiddlewares
{
    /// <summary>
    /// This middleware is used to capture the request body. It is used by the Request Logger tool.
    /// </summary>
    /// <param name="app"></param>
    public static void UseRequestBodyCapture(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();

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
                //Ignore any errors here
            }

            context.Request.Body.Position = 0;

            await next();
        });
    }

    public const string RawRequestDetailsKey = "RawRequestDetails";

    /// <summary>
    /// Captures the raw request information (scheme, host, path, query, and RawTarget)
    /// and stores it in HttpContext.Items[RawRequestDetailsKey].
    /// </summary>
    public static IApplicationBuilder UseCaptureRawRequestDetails(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Reconstruct URL as it appears before ForwardedHeaders middleware
            var rawUrl = $"{context.Request.Scheme}://{context.Request.Host}" +
                         $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

            // Capture all request headers
            var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            // Store these in HttpContext.Items so they can be retrieved later
            context.Items[RawRequestDetailsKey] = new RawRequestDetails
            {
                RawUrl = rawUrl,
                RawIncomingHeaders = headers
            };

            await next.Invoke();
        });
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
        /// All request headers as received by the application and before ForwardedHeaders makes any modifications.
        /// </summary>
        public Dictionary<string, string>? RawIncomingHeaders { get; set; }
    }
}
