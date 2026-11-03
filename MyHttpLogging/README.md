# MyHttpLogging

An instrumented copy of ASP.NET Core HTTP Logging middleware, used by CloudDebugger to display HTTP requests and responses.

The middleware uses `Microsoft.AspNetCore.MyHttpLogging` and preserves custom request-log storage, separate request/response headers, and captured request/response bodies. Local patches are marked with `Hack:` comments.

Headers and bodies can contain sensitive information. This library is intended for learning and controlled debugging.

## Source

Synchronized with **ASP.NET Core 10.0.13**, using `aspnetcore-10.0.13.zip`, including the middleware and supporting shared sources.

[Original source](https://github.com/dotnet/aspnetcore/tree/v10.0.13/src/Middleware/HttpLogging)

## Build

From the repository root:

```powershell
dotnet build CloudDebugger.sln
```

## Version history

- November 2026: synchronized with ASP.NET Core 10.0.13.
- January 2026: updated to ASP.NET Core 10.0.1 and .NET 10.

Upstream code is licensed to the .NET Foundation under the MIT license.
