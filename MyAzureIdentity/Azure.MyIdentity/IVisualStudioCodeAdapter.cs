// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    internal interface IVisualStudioCodeAdapter
    {
        string GetUserSettingsPath();
        string GetCredentials(string serviceName, string accountName);
    }
}
