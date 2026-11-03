// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

using System;
using System.Collections.Generic;
using System.Text;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    /// <summary>
    /// Options used to configure the <see cref="WorkloadIdentityCredential"/>.
    /// </summary>
    public class WorkloadIdentityCredentialOptions : TokenCredentialOptions, ISupportsDisableInstanceDiscovery, ISupportsAdditionallyAllowedTenants, ISupportsTenantId
    {
        /// <summary>
        /// The tenant ID of the service principal.
        /// </summary>
        /// <remarks>
        /// Defaults to the value of environment variable <c>AZURE_TENANT_ID</c>.
        /// </remarks>
        public string TenantId { get; set; } = EnvironmentVariables.TenantId;

        /// <summary>
        /// The client (application) ID of the service principal.
        /// </summary>
        /// <remarks>
        /// Defaults to the value of environment variable <c>AZURE_CLIENT_ID</c>.
        /// </remarks>
        public string ClientId { get; set; } = EnvironmentVariables.ClientId;

        /// <summary>
        /// The path to a file containing the workload identity token.
        /// </summary>
        /// <remarks>
        /// Defaults to the value of environment variable <c>AZURE_FEDERATED_TOKEN_FILE</c>.
        /// </remarks>
        public string TokenFilePath { get; set; } = EnvironmentVariables.AzureFederatedTokenFile;

        /// <inheritdoc />
        public bool DisableInstanceDiscovery { get; set; }

        /// <summary>
        /// Enables Azure Kubernetes Service identity binding mode to work around Entra's limit on federated identity credentials.
        /// When enabled and proxy configuration environment variables are set, requests are sent to the AKS proxy instead of directly to Entra ID.
        /// This feature is not supported when using <see cref="DefaultAzureCredential"/> or <see cref="ManagedIdentityCredential"/>.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>. The AKS identity binding and Kubernetes permissions must be configured separately.
        /// </remarks>
        public bool IsAzureProxyEnabled { get; set; }

        /// <summary>
        /// Specifies tenants in addition to the specified <see cref="TenantId"/> for which the credential may acquire tokens.
        /// Add the wildcard value "*" to allow the credential to acquire tokens for any tenant the logged in account can access.
        /// If no value is specified for <see cref="TenantId"/>, this option will have no effect, and the credential will acquire tokens for any requested tenant.
        /// </summary>
        /// <remarks>
        /// Defaults to the value of environment variable <c>AZURE_ADDITIONALLY_ALLOWED_TENANTS</c>.
        /// </remarks>
        public IList<string> AdditionallyAllowedTenants { get; internal set; } = EnvironmentVariables.AdditionallyAllowedTenants;

        internal CredentialPipeline Pipeline { get; set; }

        internal MsalConfidentialClient MsalClient { get; set; }
    }
}
