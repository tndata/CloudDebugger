// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    internal interface ISupportsTokenCachePersistenceOptions
    {
        /// <summary>
        /// Specifies the <see cref="TokenCachePersistenceOptions"/> to be used by the credential. If no options are specified, the token cache will not be persisted to disk.
        /// </summary>
        TokenCachePersistenceOptions TokenCachePersistenceOptions { get; set; }
    }
}
