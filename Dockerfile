FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src 

# Phase #1, copy solution + csproj and restore as distinct layers  ----

COPY *.sln .

COPY MyHttpLogging MyHttpLogging/.
RUN dotnet restore MyHttpLogging/MyHttpLogging.csproj

COPY MyAzureIdentity MyAzureIdentity/.
RUN dotnet restore MyAzureIdentity/MyAzureIdentity.csproj

COPY CloudDebugger CloudDebugger/.
RUN dotnet restore CloudDebugger/CloudDebugger.csproj

RUN dotnet publish CloudDebugger/CloudDebugger.csproj -c Release -o /app --no-restore 

# Phase #2, Build final image ------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS release

WORKDIR /app

COPY --from=build /app .

EXPOSE 8080

ENV ASPNETCORE_URLS http://+:8080

ENTRYPOINT ["dotnet", "./CloudDebugger.dll"]
