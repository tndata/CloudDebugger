# MyHttpLogging

Contains a copy of the HttpLogging middleware. 

The original source code is found at: https://github.com/dotnet/aspnetcore/tree/main/src/Middleware/HttpLogging

It contains the following modification:
* Changed the namespace to Azure.MyHttpLogging.
* Renamed the library to MyHttpLogging 
* Introduced some tweaks to enable logging to my custom request logging class. So that I can display the request log in the debugger application.
* Created a custom ILogger to capture the logs.
* In HttpLoggingMiddleware we use my custom RequestLogger to log the request instead of the system logger.
* Added a new class RequestLogger to log the request.
* Modifications in the library code is marked with //HACK:
* Most of the changes is in the HttpLoggingMiddleware class.


### Resources
 * <a href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/" target="_blank">HTTP logging in ASP.NET Core</a>


 This library is Copyright (c) Microsoft.
