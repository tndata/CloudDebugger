using CloudDebugger.Features.WebHooks;
using CloudDebugger.Infrastructure;
using Serilog;

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

        builder.Services.AddSerilog();

        builder.Services.AddSignalR();  //Used by the advanced webhooks

        builder.AddAndApplicationInsights();

        builder.AddAndConfigureHealth();

        builder.AddAndConfigureHttpLogging();

        builder.AddAndConfigureControllersAndViews();

        builder.AddAndConfigureSession();

        builder.AddAndConfigureCORS();

        return builder.Build();
    }


    /// <summary>
    /// Add and configure the request pipeline
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Custom middleware to capture the request body, used by the request logger tool
        app.UseRequsetBodyCapture();

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
