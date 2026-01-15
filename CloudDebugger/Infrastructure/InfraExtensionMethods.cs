using CloudDebugger.Features.Health;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.MyHttpLogging;

namespace CloudDebugger.Infrastructure;

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
    public static void AddCORS(this WebApplicationBuilder builder)
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
    public static void AddSession(this WebApplicationBuilder builder)
    {
        builder.Services.AddSession(options =>
        {
            // SameAsRequest, to support HTTP for learning about SameSite cookies
            // and observing the differences between HTTP and HTTPS behavior
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
    }

    /// <summary>
    /// Add controllers and configure the razor view engine
    /// 
    /// Enable the use fo the Feature folder structure.
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureControllersAndViews(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
    public static void AddConfigureHttpLogging(this WebApplicationBuilder builder)
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
    /// Configure the public health endpoint system and the custom health check. 
    /// The health endpoints /health and /healthz are defined in the HostingExtension class.
    /// </summary>
    /// <param name="builder"></param>
    public static void AddConfigureHealth(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
                        .AddCheck<AppHealthCheck>("CloudDebuggerHealthCheck");
    }
}
