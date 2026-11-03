// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    /// <summary>
    /// Options used to configure the <see cref="ClientAssertionCredential"/>.
    /// </summary>
    public class ClientAssertionCredentialOptions : TokenCredentialOptions, ISupportsDisableInstanceDiscovery, ISupportsAdditionallyAllowedTenants, ISupportsTokenCachePersistenceOptions, ISupportsTokenRequestCallback
    {
        internal CredentialPipeline Pipeline { get; set; }

        internal MsalConfidentialClient MsalClient { get; set; }

        internal MsalConfidentialClient PopMsalClient { get; set; }

        /// <summary>
        /// When set, the credential supports mTLS proof-of-possession and creates a proof-of-possession client.
        /// Used by the managed-identity-as-federated-identity flow to honor <c>EnableMtlsProofOfPossession</c>.
        /// </summary>
        internal bool EnableMtlsProofOfPossession { get; set; }

        /// <summary>
        /// For multi-tenant applications, specifies additional tenants for which the credential may acquire tokens. Add the wildcard value "*" to allow the credential to acquire tokens for any tenant in which the application is installed.
        /// </summary>
        public IList<string> AdditionallyAllowedTenants { get; internal set; } = new List<string>();

        /// <inheritdoc/>
        public bool DisableInstanceDiscovery { get; set; }

        /// <inheritdoc/>
        public TokenCachePersistenceOptions TokenCachePersistenceOptions { get; set; }

        /// <inheritdoc/>
        [Experimental("AZID0003")]
        public Action<TokenRequestCallbackContext> TokenRequestCallback { get; set; }
    }
}
