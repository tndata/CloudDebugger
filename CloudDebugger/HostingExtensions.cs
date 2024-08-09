using CloudDebugger.Features.Health;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.MyHttpLogging;
using Serilog;
using System.Text;

namespace CloudDebugger;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplicationInsightsTelemetry(o =>
        {
            //Options https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/NETCORE/src/Shared/Extensions/ApplicationInsightsServiceOptions.cs
            o.EnableDebugLogger = true;
        });

        builder.Services.AddSerilog();

        builder.Services.AddHealthChecks().AddCheck<AppHealthCheck>("CloudDebuggerHealthCheck");

        builder.Services.AddMyHttpLogging(o =>
        {
            o.LoggingFields = HttpLoggingFields.All;
            o.MediaTypeOptions.AddText("application/json");
            o.RequestBodyLogLimit = 2048;
            o.ResponseBodyLogLimit = 2048;
            o.CombineLogs = true;
        });

        //Support features folder structure
        builder.Services.Configure<RazorViewEngineOptions>(rvo =>
        {
            rvo.ViewLocationFormats.Add("~/Features/{1}/{0}.cshtml");
            rvo.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
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

        app.UseStaticFiles();

        app.UseMyHttpLogging();

        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health");

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
