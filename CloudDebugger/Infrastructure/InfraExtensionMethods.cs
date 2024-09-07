using CloudDebugger.Features.Health;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.MyHttpLogging;

namespace CloudDebugger;

/// <summary>
/// Extension methods to configure the various services used in this application
/// </summary>
public static class InfraExtensionMethods
{
    /// <summary>
    /// Configure CORS
    /// 
    /// Accept all origins
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndConfigureCORS(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "MyCorsPolicy_wildcard",
                                builder =>
                                {
                                    builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                                });
        });
    }


    /// <summary>
    /// Enable the session middlware, used to store temp data that is unique per browser session
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndConfigureSession(this WebApplicationBuilder builder)
    {
        builder.Services.AddSession(options =>
        {
            // We do want to support session over HTTP as well
            // Remember, the session is lost across restarts, the data is stored in memory.
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });
    }

    /// <summary>
    /// Add controllers and configure the razor view engine
    /// 
    /// Enable the use fo the Feature folder structure.
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndConfigureControllersAndViews(this WebApplicationBuilder builder)
    {
        //Support features folder structure
        builder.Services.Configure<RazorViewEngineOptions>(rvo =>
        {
            rvo.ViewLocationFormats.Add("~/Features/{1}/{0}.cshtml");
            rvo.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.WriteIndented = true; //Return pretty JSON in the APIs
        });
    }

    /// <summary>
    /// Add the customized version of the HttpLogging middleware that is part of this solution. 
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndConfigureHttpLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddMyHttpLogging(o =>
        {
            o.LoggingFields = HttpLoggingFields.All;
            o.MediaTypeOptions.AddText("application/json");
            o.RequestBodyLogLimit = 2048;
            o.ResponseBodyLogLimit = 2048;
            o.CombineLogs = true;
        });
    }

    /// <summary>
    /// Configure the public health endpoint system and the customf health check
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndConfigureHealth(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
                        .AddCheck<AppHealthCheck>("CloudDebuggerHealthCheck");
    }

    /// <summary>
    /// Configure application insighhts
    /// </summary>
    /// <param name="builder"></param>
    public static void AddAndApplicationInsights(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplicationInsightsTelemetry(o =>
        {
            //Options https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/NETCORE/src/Shared/Extensions/ApplicationInsightsServiceOptions.cs
            o.EnableDebugLogger = true;
        });
    }
}
