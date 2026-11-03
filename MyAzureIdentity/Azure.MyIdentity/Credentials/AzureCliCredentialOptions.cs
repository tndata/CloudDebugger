// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

using System;
using System.Collections.Generic;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    /// <summary>
    /// Options for configuring the <see cref="AzureCliCredential"/>.
    /// </summary>
    public class AzureCliCredentialOptions : TokenCredentialOptions, ISupportsAdditionallyAllowedTenants, ISupportsTenantId
    {
        private string _subscription;

        /// <summary>
        /// The ID of the tenant to which the credential will authenticate by default. If not specified, the credential will authenticate to any requested tenant, and will default to the tenant provided to the 'az login' command.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Specifies tenants in addition to the specified <see cref="TenantId"/> for which the credential may acquire tokens.
        /// Add the wildcard value "*" to allow the credential to acquire tokens for any tenant the logged in account can access.
        /// If no value is specified for <see cref="TenantId"/> this option will have no effect, and the credential will acquire tokens for any requested tenant.
        /// </summary>
        public IList<string> AdditionallyAllowedTenants { get; internal set; } = new List<string>();

        /// <summary>
        /// The Cli process timeout.
        /// </summary>
        public TimeSpan? ProcessTimeout { get; set; }

        /// <summary>
        /// The subscription name or Id to use for authentication. This equates to the --subscription parameter in the Azure CLI.
        /// </summary>
        /// <remarks>
        /// The Azure CLI does not allow the <c>--subscription</c> and <c>--tenant</c> arguments to be specified together.
        /// When a tenant is requested for a token — for example, one surfaced by challenge-based authentication — that tenant
        /// is authoritative for the issued token and takes precedence, so this <see cref="Subscription"/> value is not passed
        /// to the CLI for that request. It is used only when no tenant is requested.
        /// </remarks>
        public string Subscription
        {
            get => _subscription;
            set
            {
                if (!Validations.IsValidateSubscriptionNameOrId(value))
                {
                    throw new ArgumentException("The provided subscription contains invalid characters. If this is the name of a subscription, use its ID instead.");
                }

                _subscription = value;
            }
        }
    }
}
