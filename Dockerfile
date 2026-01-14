FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src 

# Phase #1, copy solution + csproj and restore as distinct layers  ----

COPY *.sln .
COPY .editorconfig CloudDebugger/

# Copy and restore MyHttpLogging project
COPY MyHttpLogging/*.csproj MyHttpLogging/
RUN dotnet restore MyHttpLogging/MyHttpLogging.csproj

# Copy and restore MyAzureIdentity project
COPY MyAzureIdentity/*.csproj MyAzureIdentity/
RUN dotnet restore MyAzureIdentity/MyAzureIdentity.csproj

# Copy and restore CloudDebugger project
COPY CloudDebugger/*.csproj CloudDebugger/
RUN dotnet restore CloudDebugger/CloudDebugger.csproj

# Copy source files for each project explicitly
COPY MyHttpLogging/ MyHttpLogging/
COPY MyAzureIdentity/ MyAzureIdentity/
COPY CloudDebugger/ CloudDebugger/

RUN dotnet publish CloudDebugger/CloudDebugger.csproj -c Release -o /app 

# Phase #2, Build final image ------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS release

# Switch to non-root user for security
USER app

WORKDIR /app

COPY --from=build /app .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "./CloudDebugger.dll"]
