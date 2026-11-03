// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    internal static class AbstractAcquireTokenParameterBuilderExtensions
    {
        public static ValueTask<AuthenticationResult> ExecuteAsync<T>(this AbstractAcquireTokenParameterBuilder<T> builder, bool async, CancellationToken cancellationToken)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            // Hack: Measure MSAL execution with the same facts across credential types.
            return ExecuteLoggedAsync(() => builder.ExecuteAsync(cancellationToken), builder.GetType().Name, async);
        }

        // Hack: MSAL's managed-identity builder does not inherit AbstractAcquireTokenParameterBuilder.
        internal static ValueTask<AuthenticationResult> ExecuteAsync(this AcquireTokenForManagedIdentityParameterBuilder builder, bool async, CancellationToken cancellationToken)
        {
            return ExecuteLoggedAsync(() => builder.ExecuteAsync(cancellationToken), builder.GetType().Name, async);
        }

        // Hack: Preserve the existing synchronous/asynchronous execution and exception behavior.
        private static async ValueTask<AuthenticationResult> ExecuteLoggedAsync(Func<Task<AuthenticationResult>> execute, string operation, bool async)
        {
            var elapsed = Stopwatch.StartNew();
            try
            {
                AuthenticationResult result = async
                    ? await execute().ConfigureAwait(false)
#pragma warning disable AZC0102 // Preserve synchronous MSAL execution.
                    : execute().GetAwaiter().GetResult();
#pragma warning restore AZC0102

                MyAzureIdentityLog.AddToLog("MSAL", $"{operation} succeeded in {elapsed.ElapsedMilliseconds} ms\n{IdentityDiagnosticLogging.DescribeResult(result)}");
                return result;
            }
            catch (Exception e)
            {
                MyAzureIdentityLog.AddToLog("MSAL", $"{operation} failed in {elapsed.ElapsedMilliseconds} ms: {(e as MsalException)?.ErrorCode ?? e.GetType().Name}");
                throw;
            }
        }
    }
}
