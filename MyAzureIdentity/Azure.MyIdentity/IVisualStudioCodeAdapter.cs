﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Azure.MyIdentity
{
    internal interface IVisualStudioCodeAdapter
    {
        string GetUserSettingsPath();
        string GetCredentials(string serviceName, string accountName);
    }
}
