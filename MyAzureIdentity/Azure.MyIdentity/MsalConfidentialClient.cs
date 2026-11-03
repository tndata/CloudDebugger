// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    [UnsupportedOSPlatform("browser")]
    internal class MsalConfidentialClient : MsalClientBase<IConfidentialClientApplication>
    {
        internal readonly string _clientSecret;
        internal readonly bool _includeX5CClaimHeader;
        internal readonly IX509Certificate2Provider _certificateProvider;
        private readonly Func<string> _clientAssertionCallback;
        private readonly Func<CancellationToken, Task<string>> _clientAssertionCallbackAsync;
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _clientSignedAssertionCallbackAsync;
        private readonly Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> _appTokenProviderCallback;
        internal readonly bool _enableMtlsProofOfPossession;

        internal string RedirectUrl { get; }

        // HACK: Add logging
        private static StringBuilder log = new StringBuilder();

        public string GetLog() => log.ToString();

        /// <summary>
        /// Hack: Custom Code for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MsalConfidentialClient");
            sb.AppendLine($" - ClientId: {ClientId}");
            sb.AppendLine($" - TenantId: {TenantId}");
            sb.AppendLine($" - AuthorityHost: {AuthorityHost?.AbsoluteUri}");
            sb.AppendLine($" - RegionalAuthority: {RegionalAuthority ?? "Not set"}");

            // Hack: Show whether this confidential client is configured for certificate-bound tokens.
            sb.AppendLine($" - mTLS Proof-of-Possession enabled: {_enableMtlsProofOfPossession}");

            // Show auth method type
            if (!string.IsNullOrEmpty(_clientSecret))
            {
                sb.AppendLine(" - AuthMethod: ClientSecret (secret hidden)");
            }
            else if (_certificateProvider != null)
            {
                sb.AppendLine(" - AuthMethod: Certificate");
                sb.AppendLine($" - IncludeX5C: {_includeX5CClaimHeader}");
            }
            else if (_clientAssertionCallback != null)
            {
                sb.AppendLine(" - AuthMethod: ClientAssertion (sync callback)");
            }
            else if (_clientAssertionCallbackAsync != null)
            {
                sb.AppendLine(" - AuthMethod: ClientAssertion (async callback)");
            }
            else if (_clientSignedAssertionCallbackAsync != null)
            {
                // Hack: Identify the PoP assertion callback instead of displaying "None configured".
                sb.AppendLine(" - AuthMethod: ClientSignedAssertion (async callback, mTLS PoP)");
            }
            else if (_appTokenProviderCallback != null)
            {
                sb.AppendLine(" - AuthMethod: AppTokenProvider (Managed Identity)");
            }
            else
            {
                sb.AppendLine(" - AuthMethod: None configured");
            }

            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                sb.AppendLine($" - RedirectUrl: {RedirectUrl}");
            }

            sb.AppendLine($" - DisableInstanceDiscovery: {DisableInstanceDiscovery}");
            sb.AppendLine($" - IsSupportLoggingEnabled: {IsSupportLoggingEnabled}");

            if (log.Length > 0)
            {
                sb.AppendLine("");
                sb.AppendLine("=== Operation Log ===");
                sb.AppendLine(log.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// For mocking purposes only.
        /// </summary>
        protected MsalConfidentialClient()
        { }

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, string clientSecret, string redirectUrl, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _clientSecret = clientSecret;
            RedirectUrl = redirectUrl;
        }

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, IX509Certificate2Provider certificateProvider, bool includeX5CClaimHeader, TokenCredentialOptions options, bool enableMtlsProofOfPossession = false)
            : base(pipeline, tenantId, clientId, options)
        {
            _includeX5CClaimHeader = includeX5CClaimHeader;
            _certificateProvider = certificateProvider;
            _enableMtlsProofOfPossession = enableMtlsProofOfPossession;
        }

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, Func<string> assertionCallback, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _clientAssertionCallback = assertionCallback;
        }

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, Func<CancellationToken, Task<string>> assertionCallback, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _clientAssertionCallbackAsync = assertionCallback;
        }

        internal MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> assertionCallback, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _clientSignedAssertionCallbackAsync = assertionCallback;
            _enableMtlsProofOfPossession = true;
        }

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> appTokenProviderCallback, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _appTokenProviderCallback = appTokenProviderCallback;
        }

        internal string RegionalAuthority { get; } = EnvironmentVariables.AzureRegionalAuthorityName;

        protected override ValueTask<IConfidentialClientApplication> CreateClientAsync(bool enableCae, bool async, CancellationToken cancellationToken)
        {
            return CreateClientCoreAsync(enableCae, async, cancellationToken);
        }

        protected virtual async ValueTask<IConfidentialClientApplication> CreateClientCoreAsync(bool enableCae, bool async, CancellationToken cancellationToken)
        {
            // HACK: Log client creation
            log.AppendLine($"Creating ConfidentialClientApplication - EnableCAE: {enableCae}, Async: {async}");


            string[] clientCapabilities =
                enableCae ? cp1Capabilities : Array.Empty<string>();

            ConfidentialClientApplicationBuilder confClientBuilder = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithLogging(AzureIdentityEventSource.Singleton, enablePiiLogging: IsSupportLoggingEnabled);

            if (!_enableMtlsProofOfPossession)
            {
                confClientBuilder.WithHttpClientFactory(new HttpPipelineClientFactory(Pipeline.HttpPipeline), false);
            }

            // Special case for using appTokenProviderCallback, authority validation and instance metadata discovery should be disabled since we're not calling the STS
            // The authority matches the one configured in the CredentialOptions.
            if (_appTokenProviderCallback != null)
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine(" - Using AppTokenProvider (Managed Identity mode)");

                confClientBuilder.WithAppTokenProvider(_appTokenProviderCallback)
                    .WithAuthority(AuthorityHost.AbsoluteUri, TenantId, false)
                    .WithInstanceDiscovery(false);
            }
            else
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine($" - Using standard authority: {AuthorityHost.AbsoluteUri}");

                confClientBuilder.WithAuthority(AuthorityHost.AbsoluteUri, TenantId);
                if (DisableInstanceDiscovery)
                {
                    // Hack: Record internal credential activity for the learning UI.
                    log.AppendLine(" - Instance discovery disabled");
                    confClientBuilder.WithInstanceDiscovery(false);
                }
            }

            if (clientCapabilities.Length > 0)
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine($" - Client capabilities: {string.Join(", ", clientCapabilities)}");

                confClientBuilder.WithClientCapabilities(clientCapabilities);
            }

            if (_clientSecret != null)
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine(" - Configuring client secret authentication");
                confClientBuilder.WithClientSecret(_clientSecret);
            }

            if (_clientAssertionCallback != null)
            {
                if (_clientAssertionCallbackAsync != null)
                {
                    throw new InvalidOperationException($"Cannot set both {nameof(_clientAssertionCallback)} and {nameof(_clientAssertionCallbackAsync)}");
                }
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine(" - Configuring client assertion (sync)");
                confClientBuilder.WithClientAssertion(_clientAssertionCallback);
            }

            if (_clientAssertionCallbackAsync != null)
            {
                if (_clientAssertionCallback != null)
                {
                    throw new InvalidOperationException($"Cannot set both {nameof(_clientAssertionCallback)} and {nameof(_clientAssertionCallbackAsync)}");
                }
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine(" - Configuring client assertion (async)");
                confClientBuilder.WithClientAssertion(_clientAssertionCallbackAsync);
            }

            if (_clientSignedAssertionCallbackAsync != null)
            {
                confClientBuilder.WithClientAssertion(_clientSignedAssertionCallbackAsync);
            }

            if (_certificateProvider != null)
            {
                X509Certificate2 clientCertificate = await _certificateProvider.GetCertificateAsync(async, cancellationToken).ConfigureAwait(false);
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine(" - Configuring certificate authentication");
                log.AppendLine($"   - Certificate Subject: {clientCertificate?.Subject}");
                log.AppendLine($"   - Certificate Thumbprint: {clientCertificate?.Thumbprint}");

                confClientBuilder.WithCertificate(clientCertificate);
            }

            // When the appTokenProviderCallback is set, meaning this is for managed identity, the regional authority is not relevant.
            if (_appTokenProviderCallback == null && !string.IsNullOrEmpty(RegionalAuthority))
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine($" - Configuring regional authority: {RegionalAuthority}");
                confClientBuilder.WithAzureRegion(RegionalAuthority);
            }

            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine($" - Configuring redirect URL: {RedirectUrl}");
                confClientBuilder.WithRedirectUri(RedirectUrl);
            }

            if (AdditionalQueryParameters != null)
            {
                confClientBuilder.WithExtraQueryParameters(AdditionalQueryParameters.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Value, kvp.Value.IncludeInCacheKey)));
            }

            return confClientBuilder.Build();
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenForClientAsync(
            string[] scopes,
            string tenantId,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {

            // HACK: Log the request
            log.AppendLine($"AcquireTokenForClient - Scopes: {string.Join(", ", scopes)}");
            log.AppendLine($" - TenantId: {tenantId ?? "default"}");
            log.AppendLine($" - Claims: {(string.IsNullOrEmpty(claims) ? "none" : "present")}");
            log.AppendLine($" - EnableCAE: {enableCae}, Async: {async}");


            // Hack: Identify actual requests through the PoP client, including cached-client reuse.
            log.AppendLine($" - Token acquisition mode: {(_enableMtlsProofOfPossession ? "mTLS Proof-of-Possession (PopClient)" : "standard client")}");
            if (_enableMtlsProofOfPossession)
            {
                MyAzureIdentityLog.AddToLog("MsalConfidentialClient", "PopClient used: requesting an mTLS proof-of-possession token.");
            }

            AuthenticationResult result;
            try
            {
                result = await AcquireTokenForClientCoreAsync(scopes, tenantId, claims, enableCae, async, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (_enableMtlsProofOfPossession)
            {
                // Hack: Keep failed PoP attempts visible even when the caller subsequently retries with its regular client.
                string errorCode = (e as MsalException)?.ErrorCode ?? e.GetType().Name;
                string message = $"PopClient token acquisition failed: {errorCode}";
                log.AppendLine($" - {message}");
                MyAzureIdentityLog.AddToLog("MsalConfidentialClient", message);
                throw;
            }

            // HACK: Log the result
            if (result != null)
            {
                // Hack: Keep shared token facts visible through the existing confidential-client operation log.
                log.AppendLine(IdentityDiagnosticLogging.DescribeResult(result));
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Success! Token acquired");
                // Hack: Distinguish requesting PoP from the token type actually returned by MSAL.
                if (_enableMtlsProofOfPossession)
                {
                    log.AppendLine($"   - PopClient acquisition succeeded; returned token type: {result.TokenType}");
                }
                log.AppendLine($"   - ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(result.ExpiresOn)}");
                log.AppendLine($"   - Lifetime: {((int)lifetime.TotalMinutes)} minutes");
                log.AppendLine($"   - Cached: {result.AuthenticationResultMetadata?.TokenSource == TokenSource.Cache}");

                // Optionally log the token for debugging
                // log.AppendLine($"   - AccessToken: {result.AccessToken}");

                // Hack: Record internal credential activity for the learning UI.
                MyAzureIdentityLog.AddToLog("MsalConfidentialClient", $"AcquireTokenForClient\r\n{log.ToString()}");
            }

            LogAccountDetails(result);
            return result;
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenForClientCoreAsync(
            string[] scopes,
            string tenantId,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            IConfidentialClientApplication client = await GetClientAsync(enableCae, async, cancellationToken).ConfigureAwait(false);

            var builder = client
                .AcquireTokenForClient(scopes)
                .WithSendX5C(_includeX5CClaimHeader);

            if (_enableMtlsProofOfPossession)
            {
                // Hack: Record that the MSAL request builder actually enables mTLS PoP.
                log.AppendLine(" - MSAL request configured with mTLS Proof-of-Possession");
                builder.WithMtlsProofOfPossession();
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }
            if (!string.IsNullOrEmpty(claims))
            {
                builder.WithClaims(claims);
            }
            ApplyTokenRequestCallback(builder);
            return await builder
                .ExecuteAsync(async, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenSilentAsync(
            string[] scopes,
            AuthenticationAccount account,
            string tenantId,
            string redirectUri,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            // HACK: Log silent token request
            log.AppendLine($"AcquireTokenSilent - Scopes: {string.Join(", ", scopes)}");
            log.AppendLine($" - TenantId: {tenantId ?? "default"}");

            var result = await AcquireTokenSilentCoreAsync(scopes, account, tenantId, redirectUri, claims, enableCae, async, cancellationToken).ConfigureAwait(false);

            // HACK: Log result
            if (result != null)
            {
                // Hack: Keep shared token facts visible through the existing confidential-client operation log.
                log.AppendLine(IdentityDiagnosticLogging.DescribeResult(result));
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Silent acquisition successful");
                log.AppendLine($"   - ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(result.ExpiresOn)}");
                log.AppendLine($"   - Lifetime: {((int)lifetime.TotalMinutes)} minutes");
                log.AppendLine($"   - From Cache: {result.AuthenticationResultMetadata?.TokenSource == TokenSource.Cache}");
            }

            LogAccountDetails(result);
            return result;
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenSilentCoreAsync(
            string[] scopes,
            AuthenticationAccount account,
            string tenantId,
            string redirectUri,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {

            IConfidentialClientApplication client = await GetClientAsync(enableCae, async, cancellationToken).ConfigureAwait(false);

            var builder = client.AcquireTokenSilent(scopes, account);
            if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }
            if (!string.IsNullOrEmpty(claims))
            {
                builder.WithClaims(claims);
            }
            ApplyTokenRequestCallback(builder);
            return await builder
                .ExecuteAsync(async, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(
            string[] scopes,
            string code,
            string tenantId,
            string redirectUri,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            // HACK: Log auth code request
            log.AppendLine($"AcquireTokenByAuthorizationCode - Scopes: {string.Join(", ", scopes)}");
            log.AppendLine($" - Code length: {code?.Length ?? 0}");
            log.AppendLine($" - TenantId: {tenantId ?? "default"}");
            log.AppendLine($" - RedirectUri: {redirectUri}");

            var result = await AcquireTokenByAuthorizationCodeCoreAsync(scopes: scopes, code: code, tenantId: tenantId, redirectUri: redirectUri, claims: claims, enableCae: enableCae, async, cancellationToken).ConfigureAwait(false);

            // HACK: Log result
            if (result != null)
            {
                // Hack: Keep shared token facts visible through the existing confidential-client operation log.
                log.AppendLine(IdentityDiagnosticLogging.DescribeResult(result));
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Authorization code exchange successful");
                log.AppendLine($"   - ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(result.ExpiresOn)}");
                log.AppendLine($"   - Lifetime: {((int)lifetime.TotalMinutes)} minutes");
            }


            LogAccountDetails(result);
            return result;
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenByAuthorizationCodeCoreAsync(
            string[] scopes,
            string code,
            string tenantId,
            string redirectUri,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            IConfidentialClientApplication client = await GetClientAsync(enableCae, async, cancellationToken).ConfigureAwait(false);

            var builder = client.AcquireTokenByAuthorizationCode(scopes, code);

            if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }
            if (!string.IsNullOrEmpty(claims))
            {
                builder.WithClaims(claims);
            }
            ApplyTokenRequestCallback(builder);
            return await builder
                .ExecuteAsync(async, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            string[] scopes,
            string tenantId,
            UserAssertion userAssertionValue,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            // HACK: Log OBO request
            log.AppendLine($"AcquireTokenOnBehalfOf - Scopes: {string.Join(", ", scopes)}");
            log.AppendLine($" - TenantId: {tenantId ?? "default"}");
            log.AppendLine($" - UserAssertion present: {userAssertionValue != null}");


            var result = await AcquireTokenOnBehalfOfCoreAsync(scopes, tenantId, userAssertionValue, claims, enableCae, async, cancellationToken).ConfigureAwait(false);


            // HACK: Log result
            if (result != null)
            {
                // Hack: Keep shared token facts visible through the existing confidential-client operation log.
                log.AppendLine(IdentityDiagnosticLogging.DescribeResult(result));
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - OBO token acquisition successful");
                log.AppendLine($"   - ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(result.ExpiresOn)}");
                log.AppendLine($"   - Lifetime: {((int)lifetime.TotalMinutes)} minutes");
                log.AppendLine($"   - Account: {result.Account?.Username ?? "N/A"}");
            }

            LogAccountDetails(result);

            return result;
        }

        public virtual async ValueTask<AuthenticationResult> AcquireTokenOnBehalfOfCoreAsync(
            string[] scopes,
            string tenantId,
            UserAssertion userAssertionValue,
            string claims,
            bool enableCae,
            bool async,
            CancellationToken cancellationToken)
        {
            IConfidentialClientApplication client = await GetClientAsync(enableCae, async, cancellationToken).ConfigureAwait(false);

            var builder = client
                .AcquireTokenOnBehalfOf(scopes, userAssertionValue)
                .WithSendX5C(_includeX5CClaimHeader);

            if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }
            if (!string.IsNullOrEmpty(claims))
            {
                builder.WithClaims(claims);
            }
            ApplyTokenRequestCallback(builder);
            return await builder
                .ExecuteAsync(async, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
