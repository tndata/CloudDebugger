// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.MyIdentity
{
    internal class BrokerCredential : InteractiveBrowserCredential
    {
        private const string Troubleshooting = "See the troubleshooting guide for more information. https://aka.ms/azsdk/net/identity/brokercredential/troubleshoot";

        private readonly bool _isBrokerOptionsEnabled;


        /// <summary>
        /// Hack: Custom Code for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("BrokerCredential");
            sb.AppendLine($" - _isBrokerOptionsEnabled = {_isBrokerOptionsEnabled}");
            sb.AppendLine("");
            return sb.ToString();
        }

        public BrokerCredential(DevelopmentBrokerOptions options)
            : base(CredentialOptionsMapper.GetBrokerOptions(out bool isBrokerEnabled, options) ?? CredentialOptionsMapper.CreateFallbackOptions(options))
        {
            _isBrokerOptionsEnabled = isBrokerEnabled;
        }

        /// <InheritDoc />
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default) =>
            GetTokenImpl(false, requestContext, cancellationToken).EnsureCompleted();

        /// <InheritDoc />
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default) =>
            await GetTokenImpl(true, requestContext, cancellationToken).ConfigureAwait(false);

        private async Task<AccessToken> GetTokenImpl(bool async, TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            if (!_isBrokerOptionsEnabled)
            {
                throw new CredentialUnavailableException($"The {nameof(BrokerCredential)} requires the Azure.Identity.Broker package to be referenced. {Troubleshooting}");
            }

            using CredentialDiagnosticScope scope = Pipeline.StartGetTokenScope($"{nameof(BrokerCredential)}.{nameof(GetToken)}", requestContext);

            try
            {
                var token = async
                    ? await base.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false)
                    : base.GetToken(requestContext, cancellationToken);
                scope.Succeeded(token);

                return token;
            }
            catch (Exception e)
            {
                throw scope.FailWrapAndThrow(e, "BrokerCredential failed to silently authenticate via the broker", isCredentialUnavailable: true);
            }
        }
    }
}
