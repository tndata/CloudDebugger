# Cloud Debugger - the exploration tool for Azure cloud developers

## About the Cloud Debugger
Cloud Debugger is an open-source tool built with ASP.NET Core 9, providing a comprehensive set of utilities for exploring, teaching, and debugging Azure services. 
Designed to be deployed to Azure App Services, it enables developers, architects, and DevOps engineers to seamlessly explore and troubleshoot Azure services and APIs. 
Cloud Debugger is a web-based tool that can be accessed from any browser, making it easy to gain insights into your cloud environment and streamline your debugging processes.

![image](https://github.com/user-attachments/assets/be9e5501-ce86-401d-8dc7-9b5a171a82d5)

## Build status
![Build status](https://github.com/tndata/CloudDebugger/actions/workflows/main.yml/badge.svg)


## To get started
See the [deployment guide](https://github.com/tndata/CloudDebugger/wiki/Deployment) for instructions on how to deploy this service to Azure.


## Features and tools

### HTTP Request and API Tools
* [Calling External APIs](https://github.com/tndata/CloudDebugger/wiki/CallingAPIs)
* [Current Request Viewer](https://github.com/tndata/CloudDebugger/wiki/CurrentRequestViewer)
* [Forwarded Headers Configuration](https://github.com/tndata/CloudDebugger/wiki/ForwardedHeadersConfiguration)
* [Load Testing](https://github.com/tndata/CloudDebugger/wiki/LoadTesting)
* [Request Logger](https://github.com/tndata/CloudDebugger/wiki/RequestLogger)
* [Request Sender](https://github.com/tndata/CloudDebugger/wiki/RequestSender)

### App Services
* [App Service File System](https://github.com/tndata/CloudDebugger/wiki/AppServices)
* [Local Cache](https://github.com/tndata/CloudDebugger/wiki/AppServices)

### Storage tools
* [Blob Storage Tools](https://github.com/tndata/CloudDebugger/wiki/BlobStorage)
* [File System Explorer](https://github.com/tndata/CloudDebugger/wiki/FileSystem)
* [Redis Explorer](https://github.com/tndata/CloudDebugger/wiki/RedisExplorer)

### Identity tools
* [Credentials Eventlog](https://github.com/tndata/CloudDebugger/wiki/CredentialsEventLog)
* [DefaultAzureCredentials](https://github.com/tndata/CloudDebugger/wiki/DefaultAzureCredentials)
* [Token Caching](https://github.com/tndata/https://github.com/tndata/CloudDebugger/wiki/TokenCaching)
* [Token Explorer](https://github.com/tndata/CloudDebugger/wiki/TokenExplorer)

### Diagnostics and system information
* [App Configuration](https://github.com/tndata/CloudDebugger/wiki/Configuration)
* [Connection Strings](https://github.com/tndata/CloudDebugger/wiki/ConnectionStrings)
* [Environment Variables](https://github.com/tndata/CloudDebugger/wiki/Diagnostics)
* [Errors](https://github.com/tndata/CloudDebugger/wiki/Errors)
* [Network Details](https://github.com/tndata/CloudDebugger/wiki/Diagnostics)
* [Runtime Details](https://github.com/tndata/CloudDebugger/wiki/Diagnostics)
* [Azure SDK Event log](https://github.com/tndata/CloudDebugger/wiki/AzureSDKEventLog)
 
### Observabilty
* [Log Analytics Workspace](https://github.com/tndata/CloudDebugger/wiki/LogWorkspace)
* [Write a message to the log](https://github.com/tndata/CloudDebugger/wiki/Logging)
* [Write to standard output and error](https://github.com/tndata/CloudDebugger/wiki/WriteToStdOutErr)
* [OpenTelemetry](https://github.com/tndata/CloudDebugger/wiki/OpenTelemetry)

### Event and messaging
* [Event Grid](https://github.com/tndata/CloudDebugger/wiki/EventGrid)
* [Event Hubs](https://github.com/tndata/CloudDebugger/wiki/EventHubs)
* [Queue Storage](https://github.com/tndata/CloudDebugger/wiki/QueueStorage)
* [WebHooks](https://github.com/tndata/CloudDebugger/wiki/Webhooks)

### Other tools
* [Caching](https://github.com/tndata/CloudDebugger/wiki/Caching)
* [Health Endpoint](https://github.com/tndata/CloudDebugger/wiki/Health)
* [Scale-out and load-balancing](https://github.com/tndata/CloudDebugger/wiki/Scaleout)



## About the author
his tool was developed by [Tore Nestenius](https://nestenius.se/), a seasoned .NET instructor and consultant with over 25 years of experience in software development and architecture. With extensive expertise in .NET, Azure, and cloud computing, Tore is passionate about helping developers and organizations build robust software solutions and optimize their development processes. A frequent speaker at conferences and user groups, Tore actively shares his knowledge and insights with the community, fostering learning and growth for developers worldwide.

* [Stack Overflow](https://stackoverflow.com/users/68490/tore-nestenius)
* [LinkedIn](https://www.linkedin.com/in/torenestenius/)
* [Blog](https://nestenius.se/)
* [Company](https://tn-data.se/)

## Videos
* [Introducing the Cloud Debugger and DefaultAzureCredentials deep dive](https://www.youtube.com/watch?v=XgtcmfZwDn4&t=40s). In this video I present the Cloud Debugger tool. 

## Presentations
* [Slides from my Cloud Debugger presentations](https://github.com/tndata/CloudDebugger/blob/main/Presentations/CloudDebugger%20-%20Presentation.pdf). This slide deck contains example slides for most of the tools.


## Related Azure blog posts by the author:
* [Deploy a container to Azure App Services using Azure CLI and user-assigned managed identity](https://nestenius.se/2024/08/27/deploy-a-container-to-azure-app-services-using-azure-cli-and-user-assigned-managed-identity/)
* [Deploy Container to Azure App Services with System-Assigned Identity](https://nestenius.se/2024/09/02/deploy-a-container-to-azure-app-services-using-a-system-assigned-identity/)
* [DefaultAzureCredentials Under the Hood](https://nestenius.se/2024/04/18/default-azure-credentials-under-the-hood/)


Microsoft, Azure and .NET are trademarks of the Microsoft group of companies.
