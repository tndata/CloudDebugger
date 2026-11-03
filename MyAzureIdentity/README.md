# MyAzureIdentity

An instrumented copy of Microsoft's Azure Identity library, used by CloudDebugger to explore how Azure credentials acquire tokens.

The library uses the `Azure.MyIdentity` namespace and adds diagnostic logging, credential inspection, and access to selected internals. Local patches are marked with `Hack:` comments.

Diagnostics can include tokens and partial secrets. This library is intended for learning and controlled debugging, not production use.

## Source

Based on the Azure SDK for .NET repository snapshot downloaded on **2 November 2026**, including the identity implementation now located in [Azure.Core](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/core/Azure.Core/src/Identity) and its supporting shared and broker sources.

This tracks the upstream development source as captured on that date, rather than a released Azure.Identity NuGet package. It may contain changes not yet available in a stable release.

## Build

From the repository root:

```powershell
dotnet build CloudDebugger.sln
```

## Version history

- November 2026: updated to the repository snapshot downloaded 2 November 2026.
- January 2026: updated to Azure.Identity 1.17.1.

Microsoft source is Copyright (c) Microsoft Corporation, licensed under MIT. See LICENSE.TXT.
