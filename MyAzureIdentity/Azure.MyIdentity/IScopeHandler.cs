// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure.Core.Pipeline;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    internal interface IScopeHandler
    {
        DiagnosticScope CreateScope(ClientDiagnostics diagnostics, string name);
        void Start(string name, in DiagnosticScope scope);
        void Dispose(string name, in DiagnosticScope scope);
        void Fail(string name, in DiagnosticScope scope, Exception exception);
    }
}
