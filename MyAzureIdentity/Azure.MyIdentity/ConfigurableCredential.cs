// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    [Experimental("SCME0002")]
    [UnsupportedOSPlatform("browser")]
    internal class ConfigurableCredential : TokenCredential
    {
        private TokenCredential _tokenCredential;
        private CredentialPipeline _pipeline;
        private AccessToken _apiKeyToken;

        public ConfigurableCredential()
            : this(new DefaultAzureCredentialOptions())
        {
        }

        public ConfigurableCredential(CredentialSettings settings)
            : this(new DefaultAzureCredentialOptions(settings, null))
        {
        }

        public ConfigurableCredential(DefaultAzureCredentialOptions options)
        {
            // Hack: Explain configuration selection without exposing API keys or other configured secrets.
            MyAzureIdentityLog.AddToLog("ConfigurableCredential", $"Configuration selection: {options.CredentialSource ?? "default"}; managed identity kind: {options.ManagedIdentityIdKind ?? "legacy/default"}");
            if (options.CredentialSource == Constants.ApiKeyCredential)
            {
                _apiKeyToken = new AccessToken(options.ApiKey, DateTimeOffset.MaxValue);
            }
            else if (options.CredentialSource == Constants.ChainedTokenCredential)
            {
                // ChainedTokenCredential source — build chain directly from Sources array.
                // Use the singleton pipeline since it's only needed for diagnostics (StartGetTokenScope).
                // Each credential in the chain creates its own pipeline from its source options.
                _pipeline = CredentialPipeline.GetInstance(null);
                _tokenCredential = new ChainedTokenCredential(ChainedTokenCredentialFactory.CreateCredentialChain(options));
            }
            else
            {
                var dac = new DefaultAzureCredential(options);
                _pipeline = dac.Pipeline;
                _tokenCredential = dac;
            }
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            if (_tokenCredential is null)
            {
                return _apiKeyToken;
            }

            using CredentialDiagnosticScope scope = _pipeline.StartGetTokenScope("ConfigurableCredential.GetToken", requestContext);

            try
            {
                return await _tokenCredential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw scope.FailWrapAndThrow(ex);
            }
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            if (_tokenCredential is null)
            {
                return _apiKeyToken;
            }

            using CredentialDiagnosticScope scope = _pipeline.StartGetTokenScope("ConfigurableCredential.GetToken", requestContext);

            try
            {
                return _tokenCredential.GetToken(requestContext, cancellationToken);
            }
            catch (Exception ex)
            {
                throw scope.FailWrapAndThrow(ex);
            }
        }
    }
}
