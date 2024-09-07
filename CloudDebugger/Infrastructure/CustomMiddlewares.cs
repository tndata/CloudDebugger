using System.Text;

namespace CloudDebugger;

public static class CustomMiddlewares
{
    public static void UseRequsetBodyCapture(this WebApplication app)
    {
        app.Use((context, next) =>
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
                    bodyStr = reader.ReadToEndAsync().Result;

                    context.Items.Add("RequestBody", bodyStr);
                }
            }
            catch (Exception)
            {
                //Ignore any errors here
            }

            context.Request.Body.Position = 0;

            return next();
        });
    }
}
