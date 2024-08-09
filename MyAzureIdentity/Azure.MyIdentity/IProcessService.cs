// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Azure.MyIdentity
{
    internal interface IProcessService
    {
        IProcess Create(ProcessStartInfo startInfo);
    }
}
