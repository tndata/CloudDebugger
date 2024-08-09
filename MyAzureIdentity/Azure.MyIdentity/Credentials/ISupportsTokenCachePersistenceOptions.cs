// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.MyIdentity
{
    internal interface ISupportsTokenCachePersistenceOptions
    {
        TokenCachePersistenceOptions TokenCachePersistenceOptions { get; set; }
    }
}
