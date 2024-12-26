using CloudDebugger.Features.WebHooks;
using CloudDebugger.Infrastructure;
using CloudDebugger.Infrastructure.OpenTelemetry;

namespace CloudDebugger;

/// <summary>
/// Configure the services and the request pipeline
/// </summary>
internal static class HostingExtensions
{
    /// <summary>
    /// Add and Configure the ASP.NET Core services
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IWebHookLog, WebHookLog>();

        builder.Services.AddSignalR();  //Used by the advanced webhooks

        builder.ConfigureOpenTelemetry();

        builder.AddConfigureHealth();

        builder.AddConfigureHttpLogging();

        builder.ConfigureControllersAndViews();

        builder.AddSession();

        builder.AddCORS();

        return builder.Build();
    }


    /// <summary>
    /// Add and configure the request pipeline. Right now we display DeveloperExceptions in the browser. In the future we might want to add app.UseExceptionHandler("/Home/Error"); in production.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Custom middleware to capture the request body, used by the UseMyHttpLogging module   
        app.UseRequestBodyCapture();
        app.UseMyHttpLogging();

        app.UseStaticFiles();

        // Used by the Health Tool
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health");

        app.UseRouting();

        app.UseCors();

        app.UseAuthorization();

        app.UseSession();

        app.MapHub<WebHookHub>("/WebHookHub");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
