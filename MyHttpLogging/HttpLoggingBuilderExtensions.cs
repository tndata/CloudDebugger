// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.MyHttpLogging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HttpLogging middleware.
/// </summary>
public static class HttpLoggingBuilderExtensions
{
    /// <summary>
    /// Adds a middleware that can log HTTP requests and responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseMyHttpLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<HttpLoggingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds a middleware that can log HTTP requests and responses for server logs in W3C format.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseMyW3CLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<W3CLoggingMiddleware>();
        return app;
    }
}
