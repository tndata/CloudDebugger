// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.MyIdentity
{
    internal class MsalConfidentialClient : MsalClientBase<IConfidentialClientApplication>
    {
        internal readonly string _clientSecret;
        internal readonly bool _includeX5CClaimHeader;
        internal readonly IX509Certificate2Provider _certificateProvider;
        private readonly Func<string> _clientAssertionCallback;
        private readonly Func<CancellationToken, Task<string>> _clientAssertionCallbackAsync;
        private readonly Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> _appTokenProviderCallback;

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

        public MsalConfidentialClient(CredentialPipeline pipeline, string tenantId, string clientId, IX509Certificate2Provider certificateProvider, bool includeX5CClaimHeader, TokenCredentialOptions options)
            : base(pipeline, tenantId, clientId, options)
        {
            _includeX5CClaimHeader = includeX5CClaimHeader;
            _certificateProvider = certificateProvider;
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
                .WithHttpClientFactory(new HttpPipelineClientFactory(Pipeline.HttpPipeline))
                .WithLogging(AzureIdentityEventSource.Singleton, enablePiiLogging: IsSupportLoggingEnabled);

            // Special case for using appTokenProviderCallback, authority validation and instance metadata discovery should be disabled since we're not calling the STS
            // The authority matches the one configured in the CredentialOptions.
            if (_appTokenProviderCallback != null)
            {
                log.AppendLine(" - Using AppTokenProvider (Managed Identity mode)");

                confClientBuilder.WithAppTokenProvider(_appTokenProviderCallback)
                    .WithAuthority(AuthorityHost.AbsoluteUri, TenantId, false)
                    .WithInstanceDiscovery(false);
            }
            else
            {
                log.AppendLine($" - Using standard authority: {AuthorityHost.AbsoluteUri}");

                confClientBuilder.WithAuthority(AuthorityHost.AbsoluteUri, TenantId);
                if (DisableInstanceDiscovery)
                {
                    log.AppendLine(" - Instance discovery disabled");
                    confClientBuilder.WithInstanceDiscovery(false);
                }
            }

            if (clientCapabilities.Length > 0)
            {
                log.AppendLine($" - Client capabilities: {string.Join(", ", clientCapabilities)}");

                confClientBuilder.WithClientCapabilities(clientCapabilities);
            }

            if (_clientSecret != null)
            {
                log.AppendLine(" - Configuring client secret authentication");
                confClientBuilder.WithClientSecret(_clientSecret);
            }

            if (_clientAssertionCallback != null)
            {
                if (_clientAssertionCallbackAsync != null)
                {
                    throw new InvalidOperationException($"Cannot set both {nameof(_clientAssertionCallback)} and {nameof(_clientAssertionCallbackAsync)}");
                }
                log.AppendLine(" - Configuring client assertion (sync)");
                confClientBuilder.WithClientAssertion(_clientAssertionCallback);
            }

            if (_clientAssertionCallbackAsync != null)
            {
                if (_clientAssertionCallback != null)
                {
                    throw new InvalidOperationException($"Cannot set both {nameof(_clientAssertionCallback)} and {nameof(_clientAssertionCallbackAsync)}");
                }
                log.AppendLine(" - Configuring client assertion (async)");
                confClientBuilder.WithClientAssertion(_clientAssertionCallbackAsync);
            }

            if (_certificateProvider != null)
            {
                X509Certificate2 clientCertificate = await _certificateProvider.GetCertificateAsync(async, cancellationToken).ConfigureAwait(false);
                log.AppendLine(" - Configuring certificate authentication");
                log.AppendLine($"   - Certificate Subject: {clientCertificate?.Subject}");
                log.AppendLine($"   - Certificate Thumbprint: {clientCertificate?.Thumbprint}");

                confClientBuilder.WithCertificate(clientCertificate);
            }

            // When the appTokenProviderCallback is set, meaning this is for managed identity, the regional authority is not relevant.
            if (_appTokenProviderCallback == null && !string.IsNullOrEmpty(RegionalAuthority))
            {
                log.AppendLine($" - Configuring regional authority: {RegionalAuthority}");
                confClientBuilder.WithAzureRegion(RegionalAuthority);
            }

            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                log.AppendLine($" - Configuring redirect URL: {RedirectUrl}");
                confClientBuilder.WithRedirectUri(RedirectUrl);
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


            var result = await AcquireTokenForClientCoreAsync(scopes, tenantId, claims, enableCae, async, cancellationToken).ConfigureAwait(false);

            // HACK: Log the result
            if (result != null)
            {
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Success! Token acquired");
                log.AppendLine($"   - ExpiresOn: {result.ExpiresOn.ToString("HH:mm:ss")}");
                log.AppendLine($"   - Lifetime: {((int)lifetime.TotalMinutes)} minutes");
                log.AppendLine($"   - Cached: {result.AuthenticationResultMetadata?.TokenSource == TokenSource.Cache}");

                // Optionally log the token for debugging
                // log.AppendLine($"   - AccessToken: {result.AccessToken}");

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

            if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }
            if (!string.IsNullOrEmpty(claims))
            {
                builder.WithClaims(claims);
            }
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
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Silent acquisition successful");
                log.AppendLine($"   - ExpiresOn: {result.ExpiresOn.ToString("HH:mm:ss")}");
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
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - Authorization code exchange successful");
                log.AppendLine($"   - ExpiresOn: {result.ExpiresOn.ToString("HH:mm:ss")}");
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
                var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine($" - OBO token acquisition successful");
                log.AppendLine($"   - ExpiresOn: {result.ExpiresOn.ToString("HH:mm:ss")}");
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
            return await builder
                .ExecuteAsync(async, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
