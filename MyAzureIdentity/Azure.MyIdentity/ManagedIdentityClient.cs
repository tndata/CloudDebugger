// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System.Text;

namespace Azure.MyIdentity
{
    internal class ManagedIdentityClient
    {
        internal const string MsiUnavailableError =
            "ManagedIdentityCredential authentication unavailable. No Managed Identity endpoint found.";

        internal Lazy<ManagedIdentitySource> _identitySource;
        private MsalConfidentialClient _msal;

        private static StringBuilder log = new StringBuilder();


        public override string ToString()
        {

            var sb = new StringBuilder();
            sb.AppendLine("ManagedIdentityClient");
            if (_identitySource != null)
                sb.AppendLine($" - _identitySource = {_identitySource.ToString()}");

            if (log.Length > 0)
                sb.AppendLine(log.ToString());

            return sb.ToString();
        }



        protected ManagedIdentityClient()
        {
        }

        public ManagedIdentityClient(CredentialPipeline pipeline, string clientId = null)
            : this(new ManagedIdentityClientOptions { Pipeline = pipeline, ClientId = clientId })
        {
        }

        public ManagedIdentityClient(CredentialPipeline pipeline, ResourceIdentifier resourceId)
            : this(new ManagedIdentityClientOptions { Pipeline = pipeline, ResourceIdentifier = resourceId })
        {
        }

        public ManagedIdentityClient(ManagedIdentityClientOptions options)
        {
            if (options.ClientId != null && options.ResourceIdentifier != null)
            {
                throw new ArgumentException(
                    $"{nameof(ManagedIdentityClientOptions)} cannot specify both {nameof(options.ResourceIdentifier)} and {nameof(options.ClientId)}.");
            }

            ClientId = string.IsNullOrEmpty(options.ClientId) ? null : options.ClientId;
            ResourceIdentifier = string.IsNullOrEmpty(options.ResourceIdentifier) ? null : options.ResourceIdentifier;
            Pipeline = options.Pipeline;
            _identitySource = new Lazy<ManagedIdentitySource>(() => SelectManagedIdentitySource(options));
            _msal = new MsalConfidentialClient(Pipeline, "MANAGED-IDENTITY-RESOURCE-TENENT", ClientId ?? "SYSTEM-ASSIGNED-MANAGED-IDENTITY", AppTokenProviderImpl, options.Options);

            //_msal
        }



        internal CredentialPipeline Pipeline { get; }

        internal protected string ClientId { get; }

        internal ResourceIdentifier ResourceIdentifier { get; }

        public async ValueTask<AccessToken> AuthenticateAsync(bool async, TokenRequestContext context, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _msal.AcquireTokenForClientAsync(context.Scopes, context.TenantId, context.Claims, context.IsCaeEnabled, async, cancellationToken).ConfigureAwait(false);


            var lifetime = result.ExpiresOn - DateTimeOffset.UtcNow;

            log.AppendLine("Got access token: " + result.AccessToken);
            log.AppendLine("ExpiresOn: " + result.ExpiresOn);
            log.AppendLine("lifetime: " + lifetime.TotalMinutes + " minutes");

            return new AccessToken(result.AccessToken, result.ExpiresOn);
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

            return new AppTokenProviderResult() { AccessToken = token.Token, ExpiresInSeconds = Math.Max(Convert.ToInt64((token.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds), 1) };
        }

        private static ManagedIdentitySource SelectManagedIdentitySource(ManagedIdentityClientOptions options)
        {
            log = new StringBuilder();

            //return
            //    ServiceFabricManagedIdentitySource.TryCreate(options) ??
            //    AppServiceV2019ManagedIdentitySource.TryCreate(options) ??
            //    AppServiceV2017ManagedIdentitySource.TryCreate(options) ??
            //    CloudShellManagedIdentitySource.TryCreate(options) ??
            //    AzureArcManagedIdentitySource.TryCreate(options) ??
            //    TokenExchangeManagedIdentitySource.TryCreate(options) ??
            //    new ImdsManagedIdentitySource(options);

            log.AppendLine(" - Trying ServiceFabricManagedIdentitySource");
            var result = ServiceFabricManagedIdentitySource.TryCreate(options);
            log.AppendLine(ServiceFabricManagedIdentitySource.TryCreateLog);
            if (result != null)
            {
                log.AppendLine("Selected ServiceFabricManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            log.AppendLine(" - Trying AppServiceV2019ManagedIdentitySource");
            result = AppServiceV2019ManagedIdentitySource.TryCreate(options);
            log.AppendLine(AppServiceV2019ManagedIdentitySource.TryCreateLog);

            if (result != null)
            {
                log.AppendLine("Selected AppServiceV2019ManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            log.AppendLine(" - Trying AppServiceV2017ManagedIdentitySource");
            result = AppServiceV2017ManagedIdentitySource.TryCreate(options);
            log.AppendLine(AppServiceV2017ManagedIdentitySource.TryCreateLog);
            if (result != null)
            {
                log.AppendLine("Selected AppServiceV2017ManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            log.AppendLine(" - Trying CloudShellManagedIdentitySource");
            result = CloudShellManagedIdentitySource.TryCreate(options);
            log.AppendLine(CloudShellManagedIdentitySource.TryCreateLog);
            if (result != null)
            {
                log.AppendLine("Selected CloudShellManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            log.AppendLine(" - Trying AzureArcManagedIdentitySource");
            result = AzureArcManagedIdentitySource.TryCreate(options);
            log.AppendLine(AzureArcManagedIdentitySource.TryCreateLog);
            if (result != null)
            {
                log.AppendLine("Selected AzureArcManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            log.AppendLine(" - Trying TokenExchangeManagedIdentitySource");
            result = TokenExchangeManagedIdentitySource.TryCreate(options);
            log.AppendLine(TokenExchangeManagedIdentitySource.TryCreateLog);
            if (result != null)
            {
                log.AppendLine("Selected TokenExchangeManagedIdentitySource");
                MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());
                return result;
            }

            // If all else fails, return a new instance of ImdsManagedIdentitySource
            log.AppendLine(" - Trying ImdsManagedIdentitySource");
            var imds = new ImdsManagedIdentitySource(options);
            log.AppendLine("Selected ImdsManagedIdentitySource");
            log.AppendLine(imds?.ToString());

            MyAzureIdentityLog.AddToLog("ManagedIdentityClient", "SelectManagedIdentitySource\r\n" + log.ToString());

            return imds;
        }
    }
}
