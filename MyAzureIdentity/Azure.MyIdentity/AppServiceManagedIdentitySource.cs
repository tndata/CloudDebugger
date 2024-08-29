// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using System.Text;

namespace Azure.MyIdentity
{
    internal class AppServiceManagedIdentitySource : ManagedIdentitySource
    {
        // MSI Constants. Docs for MSI are available here https://learn.microsoft.com/azure/app-service/overview-managed-identity
        protected virtual string AppServiceMsiApiVersion { get { throw new NotImplementedException(); } }
        protected virtual string SecretHeaderName { get { throw new NotImplementedException(); } }
        protected virtual string ClientIdHeaderName { get { throw new NotImplementedException(); } }

        public override string ToString()
        {
            var secret = "NULL";
            if (string.IsNullOrEmpty(_secret) == false)
                secret = _secret.Length <= 5 ? _secret : _secret.Substring(0, 5) + "******";

            var sb = new StringBuilder();
            sb.AppendLine($"AppServiceManagedIdentitySource");
            sb.AppendLine($" - endpoint = {_endpoint}");
            sb.AppendLine($" - clientId = {_clientId}");
            sb.AppendLine($" - resourceId = {_resourceId}");
            sb.AppendLine($" - secret = {secret}");
            sb.AppendLine($"[If this step fails, ensure the ClientId is set, for example by setting the AZURE_CLIENT_ID env variable");

            return sb.ToString();
        }


        private const string MsiEndpointInvalidUriError = "The environment variable MSI_ENDPOINT contains an invalid Uri.";

        private readonly Uri _endpoint;
        private readonly string _secret;
        private readonly string _clientId;
        private readonly string _resourceId;

        protected static bool TryValidateEnvVars(string msiEndpoint, string secret, out Uri endpointUri)
        {
            endpointUri = null;
            // if BOTH the env vars endpoint and secret values are null, this MSI provider is unavailable.
            // Also validate that IdentityServerThumbprint is null or empty to differentiate from Service Fabric.
            if (string.IsNullOrEmpty(msiEndpoint) || string.IsNullOrEmpty(secret))
            {
                return false;
            }

            try
            {
                endpointUri = new Uri(msiEndpoint);
            }
            catch (FormatException ex)
            {
                throw new AuthenticationFailedException(MsiEndpointInvalidUriError, ex);
            }

            return true;
        }

        protected AppServiceManagedIdentitySource(CredentialPipeline pipeline, Uri endpoint, string secret,
            ManagedIdentityClientOptions options) : base(pipeline)
        {
            _endpoint = endpoint;
            _secret = secret;
            _clientId = options.ClientId;
            _resourceId = options.ResourceIdentifier?.ToString();
        }

        protected override Request CreateRequest(string[] scopes)
        {
            // covert the scopes to a resource string
            string resource = ScopeUtilities.ScopesToResource(scopes);

            Request request = Pipeline.HttpPipeline.CreateRequest();

            request.Method = RequestMethod.Get;
            request.Headers.Add(SecretHeaderName, _secret);
            request.Uri.Reset(_endpoint);
            request.Uri.AppendQuery("api-version", AppServiceMsiApiVersion);
            request.Uri.AppendQuery("resource", resource);

            if (!string.IsNullOrEmpty(_clientId))
            {
                request.Uri.AppendQuery(ClientIdHeaderName, _clientId);
            }

            if (!string.IsNullOrEmpty(_resourceId))
            {
                request.Uri.AppendQuery(Constants.ManagedIdentityResourceId, _resourceId);
            }

            return request;
        }
    }
}
