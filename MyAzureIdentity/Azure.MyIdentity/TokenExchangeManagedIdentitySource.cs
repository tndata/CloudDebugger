// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;

using Azure.Core;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    [UnsupportedOSPlatform("browser")]
    internal class TokenExchangeManagedIdentitySource : ManagedIdentitySource
    {
        private FileContentsCache _tokenFileCache;
        private ClientAssertionCredential _clientAssertionCredential;

        // HACK: Add this property for logging
        public static string Log { get; private set; } = "";


        /// <summary>
        /// Hack: Custom Code for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TokenExchangeManagedIdentitySource");
            sb.AppendLine(" - " + _clientAssertionCredential?.ToString());
            sb.AppendLine(base.ToString());
            return sb.ToString();
        }

        private TokenExchangeManagedIdentitySource(CredentialPipeline pipeline, string tenantId, string clientId, string tokenFilePath)
            : base(pipeline)
        {
            _tokenFileCache = new FileContentsCache(tokenFilePath);
            _clientAssertionCredential = new ClientAssertionCredential(tenantId, clientId, _tokenFileCache.GetTokenFileContentsAsync, new ClientAssertionCredentialOptions { Pipeline = pipeline });
        }

        public static ManagedIdentitySource TryCreate(ManagedIdentityClientOptions options)
        {
            // HACK: Log the parameters
            var log = new StringBuilder();

            string tokenFilePath = EnvironmentVariables.AzureFederatedTokenFile;
            string tenantId = EnvironmentVariables.TenantId;
            string clientId = options.ManagedIdentityId?._userAssignedId ?? EnvironmentVariables.ClientId;

            // HACK: Log the parameters
            log.AppendLine($"TokenExchangeManagedIdentitySource.TryCreate called");
            log.AppendLine($"   - AzureFederatedTokenFile: {tokenFilePath ?? "Not set"}");
            log.AppendLine($"   - TenantId: {tenantId ?? "Not set"}");
            log.AppendLine($"   - ClientId: {clientId ?? "Not set"}");
            log.AppendLine($"   - ExcludeTokenExchangeManagedIdentitySource: {options.ExcludeTokenExchangeManagedIdentitySource}");

            if (options.ExcludeTokenExchangeManagedIdentitySource || string.IsNullOrEmpty(tokenFilePath) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine("   - TokenExchangeManagedIdentitySource not available");
                Log = log.ToString();

                AzureIdentityEventSource.Singleton.ManagedIdentitySourceAttempted("TokenExchangeManagedIdentitySource", false);
                return default;
            }
            // Hack: Record internal credential activity for the learning UI.
            log.AppendLine("   - TokenExchangeManagedIdentitySource is available");
            Log = log.ToString();

            AzureIdentityEventSource.Singleton.ManagedIdentitySourceAttempted("TokenExchangeManagedIdentitySource", true);
            return new TokenExchangeManagedIdentitySource(options.Pipeline, tenantId, clientId, tokenFilePath);
        }

        public async override ValueTask<AccessToken> AuthenticateAsync(bool async, TokenRequestContext context, CancellationToken cancellationToken)
        {
            return async ? await _clientAssertionCredential.GetTokenAsync(context, cancellationToken).ConfigureAwait(false) : _clientAssertionCredential.GetToken(context, cancellationToken);
        }

        protected override Request CreateRequest(string[] scopes)
        {
            throw new NotImplementedException();
        }

    }
}
