using CloudDebugger.Features.WebHooks;
using CloudDebugger.Infrastructure;
using CloudDebugger.Infrastructure.Middlewares;
using CloudDebugger.Infrastructure.OpenTelemetry;
using Microsoft.AspNetCore.HttpOverrides;

namespace CloudDebugger;

/// <summary>
/// Configure the services and the request pipeline
/// </summary>
internal static class HostingExtensions
{
    /// <summary>
    /// Configure the ASP.NET Core services
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder, string[] args)
    {
        builder.Configuration
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.Configure(context.Configuration.GetSection("Kestrel"));
        });

        builder.WebHost.UseIIS();
        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.Services.AddSingleton<IWebHookLog, WebHookLog>();

        builder.Services.AddSignalR();  //Used by the advanced webhooks

        builder.ConfigureOpenTelemetry();

        builder.AddConfigureHealth();

        builder.AddConfigureHttpLogging();

        builder.ConfigureControllersAndViews();

        builder.AddSession();

        builder.AddCORS();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;

            // We trust all hosts, networks and proxies, so that this debugger can work across
            // all supported hosting models (App Services, Container Apps, container Instances...)
            options.AllowedHosts.Clear();
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithPromptsFromAssembly()
            .WithResourcesFromAssembly()
            .WithToolsFromAssembly();

        return builder.Build();
    }


    /// <summary>
    /// Configure the request pipeline. Right now we display DeveloperExceptions in the browser. 
    /// In the future we might want to add app.UseExceptionHandler("/Home/Error"); in production.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Capture the raw incoming request,before UseForwardedHeaders modifies it.
        app.UseRawRequestCapture();

        // Custom middleware to capture the request body, used by the custom UseMyHttpLogging module   
        app.UseRequestBodyCapture();

        app.UseMyHttpLogging();

        app.UseForwardedHeaders();

        app.UseStaticFiles();

        // Used by the Health Tool
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health");

        app.UseRouting();

        app.UseCors();

        app.UseAuthorization();

        app.UseSession();

        if (GlobalSettings.PrometheusExporterEnabled)
            app.MapPrometheusScrapingEndpoint(); // Default endpoint: /metrics

        app.MapHub<WebHookHub>("/WebHookHub");

        app.MapGroup("/mcp").MapMcp();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");


        return app;
    }
}
