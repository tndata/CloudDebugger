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
    // HACK: Marked public for debugging purposes. Only called by DefaultAzureCredentials
    public class BrokerCredential : InteractiveBrowserCredential
    {
        private const string Troubleshooting = "See the troubleshooting guide for more information. https://aka.ms/azsdk/net/identity/brokercredential/troubleshoot";
        private readonly bool _isBrokerOptionsEnabled;
        private readonly DevelopmentBrokerOptions _options;
        private int _authenticationAttempts = 0;
        private DateTime? _lastSuccessfulAuth = null;
        private string _lastError = null;
        private string _lastAccountUsed = null;
        private string _credentialSource = null;

        /// <summary>
        /// Hack: Custom Code for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("BrokerCredential");
            sb.AppendLine("================");
            sb.AppendLine("Uses platform authentication brokers (WAM on Windows, Keychain on macOS) for native OS authentication instead of web browser.");

            // Configuration status
            sb.AppendLine("Configuration:");
            sb.AppendLine($"  Broker enabled: {(_isBrokerOptionsEnabled ? "✓ Yes" : "✗ No - Azure.Identity.Broker package required")}");

            if (!_isBrokerOptionsEnabled)
            {
                sb.AppendLine($"  Status: UNAVAILABLE");
                sb.AppendLine($"  Reason: Azure.Identity.Broker package not found");
                sb.AppendLine($"  Install: dotnet add package Azure.Identity.Broker");
                return sb.ToString();
            }

            // Platform information
            sb.AppendLine($"  Platform: {Environment.OSVersion.Platform}");
            sb.AppendLine($"  OS Version: {Environment.OSVersion.VersionString}");

            // Broker type and credential source based on platform
            string brokerType = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "Windows Account Manager (WAM)",
                PlatformID.Unix when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)
                    => "macOS Keychain/Authenticator",
                _ => "Platform-specific broker (or fallback to browser)"
            };
            sb.AppendLine($"  Broker type: {brokerType}");

            // Authentication statistics
            sb.AppendLine();
            sb.AppendLine("Authentication Status:");
            sb.AppendLine($"  Total attempts: {_authenticationAttempts}");

            if (_lastSuccessfulAuth.HasValue)
            {
                var timeSinceAuth = DateTime.UtcNow - _lastSuccessfulAuth.Value;
                sb.AppendLine($"  Last successful: {_lastSuccessfulAuth.Value:yyyy-MM-dd HH:mm:ss} UTC ({timeSinceAuth.TotalMinutes:F1} minutes ago)");
            }
            else
            {
                sb.AppendLine($"  Last successful: Never");
            }

            if (!string.IsNullOrEmpty(_lastAccountUsed))
            {
                sb.AppendLine($"  Account used: {_lastAccountUsed}");
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                sb.AppendLine($"  Last error: {_lastError}");
            }

            // Options details
            if (_options != null)
            {
                sb.AppendLine();
                sb.AppendLine("Options:");
                sb.AppendLine($"  ClientId: {_options.ClientId ?? "(default)"}");
                sb.AppendLine($"  TenantId: {_options.TenantId ?? "(default)"}");
                sb.AppendLine($"  RedirectUri: {_options.RedirectUri?.ToString() ?? "(default)"}");

                if (_options.DisableAutomaticAuthentication)
                {
                    sb.AppendLine($"   Automatic authentication disabled");
                }
            }

            // Current user context (if available on Windows)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                sb.AppendLine();
                sb.AppendLine("Windows Context:");
                sb.AppendLine($"  User: {Environment.UserDomainName}\\{Environment.UserName}");
                sb.AppendLine($"  Interactive: {Environment.UserInteractive}");

                // Check if Azure AD joined
                try
                {
                    var workgroup = Environment.UserDomainName;
                    if (!string.IsNullOrEmpty(workgroup) && workgroup != Environment.MachineName)
                    {
                        sb.AppendLine($"  Domain joined: Yes ({workgroup})");
                    }
                }
                catch { }
            }

            // Try to get MSAL client info if available
            sb.AppendLine();
            sb.AppendLine("MSAL Integration:");
            try
            {
                // If there's access to the underlying MSAL client through reflection or properties
                var clientProp = this.GetType().BaseType?.GetProperty("Client",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (clientProp != null)
                {
                    var client = clientProp.GetValue(this);
                    if (client != null)
                    {
                        sb.AppendLine($"  MSAL Client: Available");

                        // Try to get account information
                        var getAccountsMethod = client.GetType().GetMethod("GetAccountsAsync");
                        if (getAccountsMethod != null)
                        {
                            sb.AppendLine($"  Cached accounts: Checking...");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"  MSAL Client: Not accessible for inspection");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  MSAL Client: Cannot inspect ({ex.Message})");
            }

            return sb.ToString();
        }

        public BrokerCredential(DevelopmentBrokerOptions options)
            : base(CredentialOptionsMapper.GetBrokerOptions(out bool isBrokerEnabled, options) ?? CredentialOptionsMapper.CreateFallbackOptions(options))
        {
            _isBrokerOptionsEnabled = isBrokerEnabled;
            _options = options;
        }

        /// <InheritDoc />
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default) =>
            GetTokenImpl(false, requestContext, cancellationToken).EnsureCompleted();

        /// <InheritDoc />
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default) =>
            await GetTokenImpl(true, requestContext, cancellationToken).ConfigureAwait(false);

        private async Task<AccessToken> GetTokenImpl(bool async, TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            _authenticationAttempts++;

            if (!_isBrokerOptionsEnabled)
            {
                _lastError = "Azure.Identity.Broker package not referenced";
                throw new CredentialUnavailableException($"The {nameof(BrokerCredential)} requires the Azure.Identity.Broker package to be referenced. {Troubleshooting}");
            }

            using CredentialDiagnosticScope scope = Pipeline.StartGetTokenScope($"{nameof(BrokerCredential)}.{nameof(GetToken)}", requestContext);

            try
            {
                var token = async
                    ? await base.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false)
                    : base.GetToken(requestContext, cancellationToken);

                _lastSuccessfulAuth = DateTime.UtcNow;
                _lastError = null;

                // Try to extract account information from the token or scope
                try
                {
                    // Try to get account info from the scope using reflection
                    var scopeType = scope.GetType();
                    var accountProp = scopeType.GetProperty("AccountIdentifier",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (accountProp != null)
                    {
                        var account = accountProp.GetValue(scope);
                        if (account != null)
                        {
                            _lastAccountUsed = account.ToString();
                        }
                    }

                    // Determine source based on authentication flow
                    if (_authenticationAttempts == 1)
                    {
                        _credentialSource = "Interactive broker authentication (user prompted)";
                    }
                    else
                    {
                        _credentialSource = "Silent broker authentication (cached token)";
                    }
                }
                catch
                {
                    // If we can't extract account info, that's okay
                }

                scope.Succeeded(token);
                return token;
            }
            catch (Exception e)
            {
                _lastError = e.Message;

                // Try to determine failure reason
                if (e.Message.Contains("user_cancelled") || e.Message.Contains("cancelled"))
                {
                    _credentialSource = "Authentication cancelled by user";
                }
                else if (e.Message.Contains("no_account"))
                {
                    _credentialSource = "No cached account found - interactive auth required";
                }

                throw scope.FailWrapAndThrow(e, "BrokerCredential failed to silently authenticate via the broker", isCredentialUnavailable: true);
            }
        }
    }
}