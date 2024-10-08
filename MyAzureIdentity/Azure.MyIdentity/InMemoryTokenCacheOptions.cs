// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.MyIdentity
{
    internal class InMemoryTokenCacheOptions : UnsafeTokenCacheOptions
    {
        protected internal override Task<ReadOnlyMemory<byte>> RefreshCacheAsync()
        {
            return Task.FromResult(new ReadOnlyMemory<byte>());
        }

        protected internal override Task TokenCacheUpdatedAsync(TokenCacheUpdatedArgs tokenCacheUpdatedArgs)
        {
            return Task.CompletedTask;
        }
    }
}
