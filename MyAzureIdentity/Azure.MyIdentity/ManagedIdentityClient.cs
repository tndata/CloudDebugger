// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MSAL = Microsoft.Identity.Client.ManagedIdentity;

namespace Azure.MyIdentity
{
    internal class ManagedIdentityClient
    {
        internal const string MsiUnavailableError =
            "ManagedIdentityCredential authentication unavailable. No Managed Identity endpoint found.";

        internal Lazy<ManagedIdentitySource> _identitySource;
        private MsalConfidentialClient _msalConfidentialClient;
        private MsalManagedIdentityClient _msalManagedIdentityClient;
        private ManagedIdentitySource _tokenExchangeManagedIdentitySource;
        private bool _isChainedCredential;
        private ManagedIdentityClientOptions _options;
        private bool _probeRequestSent;

        private static StringBuilder log = new StringBuilder();

        /// <summary>
        /// Hack: Custom Code for debugging purposes.
        /// </summary>
        public override string ToString()
        {

            var sb = new StringBuilder();
            sb.AppendLine("ManagedIdentityClient");
            if (_identitySource != null)
                sb.AppendLine($" - IidentitySource = {_identitySource.ToString()}");

            if (_options != null)
            {
                sb.AppendLine(" - ManagedIdentityClientOptions.ManagedIdentityId=" + _options.ManagedIdentityId?.ToString());
                sb.AppendLine(" - ManagedIdentityClientOptions.PreserveTransport=" + _options.PreserveTransport.ToString());
                sb.AppendLine(" - ManagedIdentityClientOptions.InitialImdsConnectionTimeout=" + _options.InitialImdsConnectionTimeout?.ToString());
                sb.AppendLine(" - ManagedIdentityClientOptions.ExcludeTokenExchangeManagedIdentitySource=" + _options.ExcludeTokenExchangeManagedIdentitySource.ToString());
                sb.AppendLine(" - ManagedIdentityClientOptions.IsForceRefreshEnabled=" + _options.IsForceRefreshEnabled.ToString());
                sb.AppendLine("");
            }

            if (log.Length > 0)
                sb.AppendLine(log.ToString());

            return sb.ToString();
        }



        protected ManagedIdentityClient()
        {
        }

        public ManagedIdentityClient(CredentialPipeline pipeline, string clientId = null)
            : this(new ManagedIdentityClientOptions { Pipeline = pipeline, ManagedIdentityId = string.IsNullOrEmpty(clientId) ? ManagedIdentityId.SystemAssigned : ManagedIdentityId.FromUserAssignedClientId(clientId) })
        {
        }

        public ManagedIdentityClient(CredentialPipeline pipeline, ResourceIdentifier resourceId)
            : this(new ManagedIdentityClientOptions { Pipeline = pipeline, ManagedIdentityId = ManagedIdentityId.FromUserAssignedResourceId(resourceId) })
        {
        }

        public ManagedIdentityClient(ManagedIdentityClientOptions options)
        {
            _options = options.Clone();
            ManagedIdentityId = options.ManagedIdentityId;
            Pipeline = options.Pipeline;
            _isChainedCredential = options.Options?.IsChainedCredential ?? false;
            _msalManagedIdentityClient = new MsalManagedIdentityClient(options);
            _identitySource = new Lazy<ManagedIdentitySource>(() => SelectManagedIdentitySource(options, _msalManagedIdentityClient));
            _msalConfidentialClient = new MsalConfidentialClient(
                Pipeline,
                "MANAGED-IDENTITY-RESOURCE-TENENT",
                options.ManagedIdentityId._idType != ManagedIdentityIdType.SystemAssigned ? options.ManagedIdentityId._userAssignedId : "SYSTEM-ASSIGNED-MANAGED-IDENTITY",
                AppTokenProviderImpl,
                options.Options);
        }

        internal CredentialPipeline Pipeline { get; }

        internal ManagedIdentityId ManagedIdentityId { get; }

        public async ValueTask<AccessToken> AuthenticateAsync(bool async, TokenRequestContext context, CancellationToken cancellationToken)
        {
            AuthenticationResult result;

            MSAL.ManagedIdentitySource availableSource = ManagedIdentityApplication.GetManagedIdentitySource();

            // HACK: logging for source selection
            log.AppendLine($"ManagedIdentity source detected: {availableSource.ToString()}");
            log.AppendLine($"ManagedIdentityId type: {_options.ManagedIdentityId.ToString()}");

            AzureIdentityEventSource.Singleton.ManagedIdentityCredentialSelected(availableSource.ToString(), _options.ManagedIdentityId.ToString());

            // If the source is DefaultToImds and the credential is chained, we should probe the IMDS endpoint first.
            if (availableSource == MSAL.ManagedIdentitySource.DefaultToImds && _isChainedCredential && !_probeRequestSent)
            {
                log.AppendLine("Probing IMDS endpoint (chained credential scenario)");
                var probedFlowTokenResult = await AuthenticateCoreAsync(async, context, cancellationToken).ConfigureAwait(false);
                _probeRequestSent = true;

                // Hack: Log the probed token result
                var probeLifetime = probedFlowTokenResult.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine("IMDS probe successful - Got access token");
                log.AppendLine($"ExpiresOn: {probedFlowTokenResult.ExpiresOn.ToString("HH:mm:ss")}");
                log.AppendLine($"Lifetime: {((int)probeLifetime.TotalMinutes)} minutes (until it expires)");

                return probedFlowTokenResult;
            }

            // ServiceFabric does not support specifying user-assigned managed identity by client ID or resource ID. The managed identity selected is based on the resource configuration.
            if (availableSource == MSAL.ManagedIdentitySource.ServiceFabric && (ManagedIdentityId?._idType != ManagedIdentityIdType.SystemAssigned))
            {
                // Hack:
                log.AppendLine("ServiceFabric error: User-assigned identity not supported");
                throw new AuthenticationFailedException(Constants.MiSeviceFabricNoUserAssignedIdentityMessage);
            }

            // First try the TokenExchangeManagedIdentitySource, if it is not available, fall back to MSAL directly.
            _tokenExchangeManagedIdentitySource ??= TokenExchangeManagedIdentitySource.TryCreate(_options);
            if (default != _tokenExchangeManagedIdentitySource)
            {
                log.AppendLine("Using TokenExchangeManagedIdentitySource");

                // HACK:
                var tokenExchangeResult = await _tokenExchangeManagedIdentitySource.AuthenticateAsync(async, context, cancellationToken).ConfigureAwait(false);

                var tokenExchangeLifetime = tokenExchangeResult.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine("TokenExchange successful - Got access token");
                log.AppendLine($"ExpiresOn: {tokenExchangeResult.ExpiresOn.ToString("HH:mm:ss")}");
                log.AppendLine($"Lifetime: {((int)tokenExchangeLifetime.TotalMinutes)} minutes (until it expires)");

                return tokenExchangeResult;
            }

            try
            {
                log.AppendLine("Using MSAL ManagedIdentityClient directly");
                log.AppendLine($"Async mode: {async}");

                // The default case is to use the MSAL implementation, which does no probing of the IMDS endpoint.
                result = async ?
                    await _msalManagedIdentityClient.AcquireTokenForManagedIdentityAsync(context, cancellationToken).ConfigureAwait(false) :
                    _msalManagedIdentityClient.AcquireTokenForManagedIdentity(context, cancellationToken);

                // HACK: Log MSAL result
                var accessToken = result.ToAccessToken();
                var lifetime = accessToken.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine("MSAL authentication successful - Got access token");
                log.AppendLine($"ExpiresOn: {accessToken.ExpiresOn.ToString("HH:mm:ss")}");
                log.AppendLine($"Lifetime: {((int)lifetime.TotalMinutes)} minutes (until it expires)");


                // HACK: log the token itself if needed for debugging
                log.AppendLine($"Access token: {accessToken.Token}");

                // HACK: Add to global log
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", $"AuthenticateAsync\r\n{log.ToString()}");

            }

            // If the IMDS endpoint is not available, we will throw a CredentialUnavailableException.
            catch (MsalServiceException ex) when (HasInnerExceptionMatching(ex, e => e is RequestFailedException && e.Message.Contains("timed out")))
            {
                // HACK: Log IMDS timeout error
                log.AppendLine($"IMDS timeout error: {ex.Message}");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", $"AuthenticateAsync Error\r\n{log.ToString()}");

                // If the managed identity is not found, throw a more specific exception.
                throw new CredentialUnavailableException(MsiUnavailableError, ex);
            }

            return result.ToAccessToken();
        }

        public virtual async ValueTask<AccessToken> AuthenticateCoreAsync(bool async, TokenRequestContext context,
            CancellationToken cancellationToken)
        {
            return await _identitySource.Value.AuthenticateAsync(async, context, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AppTokenProviderResult> AppTokenProviderImpl(AppTokenProviderParameters parameters)
        {
            TokenRequestContext requestContext = new TokenRequestContext(parameters.Scopes.ToArray(), claims: parameters.Claims);

            AccessToken token = await AuthenticateCoreAsync(true, requestContext, parameters.CancellationToken).ConfigureAwait(false);

            var resfreshOn = ManagedIdentitySource.InferManagedIdentityRefreshInValue(token.ExpiresOn);
            long? refreshInSeconds = resfreshOn switch
            {
                not null => Math.Max(Convert.ToInt64((resfreshOn.Value - DateTimeOffset.UtcNow).TotalSeconds), 1),
                _ => null
            };

            return new AppTokenProviderResult()
            {
                AccessToken = token.Token,
                ExpiresInSeconds = Math.Max(Convert.ToInt64((token.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds), 1),
                RefreshInSeconds = refreshInSeconds
            };
        }

        private static ManagedIdentitySource SelectManagedIdentitySource(ManagedIdentityClientOptions options, MsalManagedIdentityClient client = null)
        {
            // HACK: OLD VERSION
            //return TokenExchangeManagedIdentitySource.TryCreate(options) ??
            //new ImdsManagedIdentityProbeSource(options, client);

            // HACK: New version with detailed logging
            log = new StringBuilder();
            log.AppendLine("=== Selecting Managed Identity Source ===");

            // Try TokenExchangeManagedIdentitySource first
            log.AppendLine("Checking for Token Exchange environment (Kubernetes/Service Fabric)...");

            var tokenExchangeSource = TokenExchangeManagedIdentitySource.TryCreate(options);

            if (tokenExchangeSource != null)
            {
                log.AppendLine("✓ Token Exchange source available - using federated identity");

                if (TokenExchangeManagedIdentitySource.Log != null)
                {
                    log.AppendLine("  Details:");
                    log.AppendLine(TokenExchangeManagedIdentitySource.Log);
                }
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", $"SelectManagedIdentitySource\r\n{log.ToString()}");
                return tokenExchangeSource;
            }
            // Fall back to ImdsManagedIdentityProbeSource
            log.AppendLine("Token Exchange not available (not in Kubernetes/Service Fabric environment)");
            log.AppendLine("Falling back to IMDS source (Azure VM/App Service/Container Instances)...");

            var imdsProbeSource = new ImdsManagedIdentityProbeSource(options, client);

            log.AppendLine("IMDS Probe source created - will auto-detect specific Azure environment");
            log.AppendLine($"  Source type: {imdsProbeSource?.GetType().Name}");

            MyAzureIdentityLog.AddToLog("ManagedIdentityClient", $"SelectManagedIdentitySource\r\n{log}");

            return imdsProbeSource;

        }




        private static bool HasInnerExceptionMatching(Exception exception, Func<Exception, bool> condition)
        {
            var current = exception;
            while (current != null)
            {
                if (condition(current))
                {
                    return true;
                }
                current = current.InnerException;
            }
            return false;
        }
    }
}
