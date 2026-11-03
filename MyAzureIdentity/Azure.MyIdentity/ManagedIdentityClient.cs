// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MtlsBindingStrength = Microsoft.Identity.Client.AppConfig.MtlsBindingStrength;
using System.Runtime.Versioning;
using Azure.Core.Pipeline;

using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MSAL = Microsoft.Identity.Client.ManagedIdentity;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    [UnsupportedOSPlatform("browser")]
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

        // Hack: Record internal credential activity for the learning UI.
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
                // Hack: Include the new PoP option in credential inspection.
                sb.AppendLine($" - EnableMtlsProofOfPossession={_options.EnableMtlsProofOfPossession}");
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
            _msalManagedIdentityClient = CreateMsalManagedIdentityClient(options);
            _identitySource = new Lazy<ManagedIdentitySource>(() => SelectManagedIdentitySource(options, _msalManagedIdentityClient));
            _msalConfidentialClient = new MsalConfidentialClient(
                Pipeline,
                "MANAGED-IDENTITY-RESOURCE-TENENT",
                options.ManagedIdentityId._idType != ManagedIdentityIdType.SystemAssigned ? options.ManagedIdentityId._userAssignedId : "SYSTEM-ASSIGNED-MANAGED-IDENTITY",
                AppTokenProviderImpl,
                options.Options);
        }

        protected virtual MsalManagedIdentityClient CreateMsalManagedIdentityClient(ManagedIdentityClientOptions options)
        {
            return options.MsalManagedIdentityClientOverride ?? new MsalManagedIdentityClient(options);
        }

        internal CredentialPipeline Pipeline { get; }

        internal ManagedIdentityId ManagedIdentityId { get; }

        public async ValueTask<AccessToken> AuthenticateAsync(bool async, TokenRequestContext context, CancellationToken cancellationToken)
        {
            AuthenticationResult result;

            _tokenExchangeManagedIdentitySource ??= TokenExchangeManagedIdentitySource.TryCreate(_options);
            if (default != _tokenExchangeManagedIdentitySource)
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine("Using TokenExchangeManagedIdentitySource");

                // HACK:
                var tokenExchangeResult = await _tokenExchangeManagedIdentitySource.AuthenticateAsync(async, context, cancellationToken).ConfigureAwait(false);

                var tokenExchangeLifetime = tokenExchangeResult.ExpiresOn - DateTimeOffset.UtcNow;
                log.AppendLine("TokenExchange successful - Got access token");
                log.AppendLine($"ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(tokenExchangeResult.ExpiresOn)}");
                log.AppendLine($"Lifetime: {((int)tokenExchangeLifetime.TotalMinutes)} minutes (until it expires)");

                return tokenExchangeResult;
            }

            MSAL.ManagedIdentitySource availableSource;
            bool isKeyGuardAvailable = false;
            // Hack: Record caller intent separately from platform capability discovery.
            log.AppendLine($"PoP requested: {context.IsProofOfPossessionEnabled}; enabled: {_options.EnableMtlsProofOfPossession}");
            bool requiresManagedIdentityCapabilities = context.IsProofOfPossessionEnabled && _options.EnableMtlsProofOfPossession;
            if (requiresManagedIdentityCapabilities)
            {
                MSAL.ManagedIdentityCapabilities capabilities;
                try
                {
#pragma warning disable AZC0106 // Non-public asynchronous method needs 'async' parameter.
                    capabilities = await _msalManagedIdentityClient.GetManagedIdentityCapabilitiesCoreAsync(async, context, cancellationToken).ConfigureAwait(false);
#pragma warning restore AZC0106 // Non-public asynchronous method needs 'async' parameter.
                }
                catch (CredentialUnavailableException)
                {
                    throw;
                }
                catch (Exception e) when (_isChainedCredential && e is not OperationCanceledException)
                {
                    AzureIdentityEventSource.Singleton.ImdsEndpointUnavailable(ImdsManagedIdentityProbeSource.GetImdsUri(), e);
                    throw new CredentialUnavailableException(MsiUnavailableError, e);
                }

                availableSource = capabilities.Source;
                isKeyGuardAvailable =
                    capabilities.IsMtlsPopSupportedByHost &&
                    capabilities.MaxSupportedBindingStrength >= MtlsBindingStrength.KeyGuard;
            }
            else
            {
#pragma warning disable CS0618 // GetManagedIdentitySource is obsolete
                availableSource = ManagedIdentityApplication.GetManagedIdentitySource();
#pragma warning restore CS0618
            }

            // Hack: Preserve source selection diagnostics after upstream capability discovery.
            log.AppendLine("Trying to authenticate");
            log.AppendLine($"ManagedIdentity source detected: {availableSource}");
            log.AppendLine($"ManagedIdentityId type: {_options.ManagedIdentityId}");
            log.AppendLine($"KeyGuard available: {isKeyGuardAvailable}");
            AzureIdentityEventSource.Singleton.ManagedIdentityCredentialSelected(availableSource.ToString(), _options.ManagedIdentityId.ToString());

            // Chained IMDS requests use the bounded Azure.Core probe before falling through. When PoP
            // capability discovery identified IMDS first, carry the KeyGuard result through that probe.
#pragma warning disable CS0618 // DefaultToImds is obsolete but still returned by the sync GetManagedIdentitySource path
            bool shouldProbeImds = availableSource == MSAL.ManagedIdentitySource.DefaultToImds ||
                availableSource == MSAL.ManagedIdentitySource.Imds ||
                (!requiresManagedIdentityCapabilities && availableSource == MSAL.ManagedIdentitySource.None);
#pragma warning restore CS0618
            try
            {
                if (shouldProbeImds && _isChainedCredential && !_probeRequestSent)
                {
                    // Hack: Retain the IMDS probe explanation in the diagnostic log.
                    log.AppendLine("Probing IMDS endpoint (chained credential scenario)");
                    var probedFlowTokenResult = await AuthenticateCoreAsync(async, context, isKeyGuardAvailable, cancellationToken).ConfigureAwait(false);
                    _probeRequestSent = true;
                    // Hack: Log the probed token result
                    var probeLifetime = probedFlowTokenResult.ExpiresOn - DateTimeOffset.UtcNow;
                    log.AppendLine("IMDS probe successful - Got access token");
                    log.AppendLine($"ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(probedFlowTokenResult.ExpiresOn)}");
                    log.AppendLine($"Lifetime: {((int)probeLifetime.TotalMinutes)} minutes (until it expires)");

                    // Hack: Publish probe success to the existing event log as well.
                    MyAzureIdentityLog.AddToLog("ManagedIdentityClient", log.ToString());
                    return probedFlowTokenResult;
                }

                // ServiceFabric does not support specifying user-assigned managed identity by client ID or resource ID. The managed identity selected is based on the resource configuration.
                if (availableSource == MSAL.ManagedIdentitySource.ServiceFabric && (ManagedIdentityId?._idType != ManagedIdentityIdType.SystemAssigned))
                {
                    // Hack: Preserve the explanation in the diagnostic log.
                    log.AppendLine("ServiceFabric error: User-assigned identity not supported");
                    throw new AuthenticationFailedException(Constants.MiSeviceFabricNoUserAssignedIdentityMessage);
                }

                // Hack: Record the selected acquisition path.
                log.AppendLine("Using MSAL ManagedIdentityClient directly");
                log.AppendLine($"Async mode: {async}");
                // The default case is to use the MSAL implementation, which does no probing of the IMDS endpoint.
                result = async ?
                    await _msalManagedIdentityClient.AcquireTokenForManagedIdentityAsync(context, isKeyGuardAvailable, cancellationToken).ConfigureAwait(false) :
                    _msalManagedIdentityClient.AcquireTokenForManagedIdentity(context, isKeyGuardAvailable, cancellationToken);
                // HACK: Log MSAL result
                var accessToken = result.ToAccessToken();
                var lifetime = accessToken.ExpiresOn - DateTimeOffset.UtcNow;
                var currentTime = DateTimeOffset.UtcNow;
                log.AppendLine("MSAL authentication successful - Got access token");
                log.AppendLine($"Current Time: {IdentityDiagnosticLogging.FormatUtc(currentTime)}");
                log.AppendLine($"ExpiresOn: {IdentityDiagnosticLogging.FormatUtc(accessToken.ExpiresOn)}");
                log.AppendLine($"Lifetime: {lifetime.TotalHours:F2} hours ({(int)lifetime.TotalMinutes} minutes)");


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
            // MSAL reports that every managed identity source was probed and none is available (for example, on a
            // developer machine with no managed identity configured). When chained, surface a CredentialUnavailableException
            // so DefaultAzureCredential continues to the next credential instead of aborting the chain.
            catch (MsalException ex) when (_isChainedCredential && ex.ErrorCode == MsalError.ManagedIdentityAllSourcesUnavailable)
            {
                AzureIdentityEventSource.Singleton.ManagedIdentitySourcesUnavailable(ex);
                throw new CredentialUnavailableException(MsiUnavailableError, ex);
            }

            return result.ToAccessToken();
        }

        public virtual async ValueTask<AccessToken> AuthenticateCoreAsync(bool async, TokenRequestContext context, bool isKeyGuardAvailable,
            CancellationToken cancellationToken)
        {
            // IMDS re-enters MSAL after its probe, so preserve the binding capability discovered before probing.
            if (_identitySource.Value is ImdsManagedIdentityProbeSource imdsSource)
            {
                return await imdsSource.AuthenticateAsync(async, context, isKeyGuardAvailable, cancellationToken).ConfigureAwait(false);
            }

            return await _identitySource.Value.AuthenticateAsync(async, context, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AppTokenProviderResult> AppTokenProviderImpl(AppTokenProviderParameters parameters)
        {
            TokenRequestContext requestContext = new TokenRequestContext(parameters.Scopes.ToArray(), claims: parameters.Claims);

            AccessToken token = await AuthenticateCoreAsync(true, requestContext, false, parameters.CancellationToken).ConfigureAwait(false);

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
            // Hack: Record internal credential activity for the learning UI.
            log.AppendLine("Checking for Token Exchange environment (Kubernetes/Service Fabric)...");

            var tokenExchangeSource = TokenExchangeManagedIdentitySource.TryCreate(options);

            if (tokenExchangeSource != null)
            {
                // Hack: Record internal credential activity for the learning UI.
                log.AppendLine("✓ Token Exchange source available - using federated identity");

                if (TokenExchangeManagedIdentitySource.Log != null)
                {
                    // Hack: Record internal credential activity for the learning UI.
                    log.AppendLine("  Details:");
                    log.AppendLine(TokenExchangeManagedIdentitySource.Log);
                }
                // Hack: Record internal credential activity for the learning UI.
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", $"SelectManagedIdentitySource\r\n{log.ToString()}");
                return tokenExchangeSource;
            }
            // Fall back to ImdsManagedIdentityProbeSource
            // Hack: Record internal credential activity for the learning UI.
            log.AppendLine("Token Exchange not available (not in Kubernetes/Service Fabric environment)");
            log.AppendLine("Falling back to IMDS source (Azure VM/App Service/Container Instances)...");

            var imdsProbeSource = new ImdsManagedIdentityProbeSource(options, client);

            // Hack: Record internal credential activity for the learning UI.
            log.AppendLine("IMDS Probe source created - will auto-detect specific Azure environment");
            log.AppendLine($"  Source type: {imdsProbeSource?.GetType().Name}");

            // Hack: Record internal credential activity for the learning UI.
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
