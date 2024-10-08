﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Azure.MyIdentity
{

    /// <summary>
    /// Options used to configure the <see cref="AuthorizationCodeCredential"/>.
    /// </summary>
    public class AuthorizationCodeCredentialOptions : TokenCredentialOptions, ISupportsDisableInstanceDiscovery, ISupportsAdditionallyAllowedTenants
    {
        /// <summary>
        /// The redirect Uri that will be sent with the GetToken request.
        /// </summary>
        public Uri RedirectUri { get; set; }

        /// <summary>
        /// For multi-tenant applications, specifies additional tenants for which the credential may acquire tokens. Add the wildcard value "*" to allow the credential to acquire tokens for any tenant in which the application is installed.
        /// </summary>
        public IList<string> AdditionallyAllowedTenants { get; internal set; } = new List<string>();

        /// <inheritdoc/>
        public bool DisableInstanceDiscovery { get; set; }
    }
}
