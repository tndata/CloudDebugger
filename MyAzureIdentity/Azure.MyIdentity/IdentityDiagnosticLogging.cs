using System;
using System.Text;
using Azure.Core;
using Microsoft.Identity.Client;

namespace Azure.MyIdentity
{
    // Hack: Common result facts for the learning UI; never include key material or new token contents.
    internal static class IdentityDiagnosticLogging
    {
        // Hack: Human-readable diagnostic timestamps, always UTC with second precision.
        internal static string FormatUtc(DateTimeOffset value)
            => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture);

        internal static string FormatUtc(DateTimeOffset? value)
            => value.HasValue ? FormatUtc(value.Value) : "not supplied";

        internal static string FormatUtc(DateTime value)
            => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture);

        internal static string DescribeToken(AccessToken token)
        {
            var text = new StringBuilder();
            text.AppendLine($"Token type: {token.TokenType ?? "unspecified"}");
            text.AppendLine($"ExpiresOn: {FormatUtc(token.ExpiresOn)}");
            text.AppendLine($"RefreshOn: {FormatUtc(token.RefreshOn)}");
            var certificate = token.BindingCertificate;
            text.AppendLine($"Binding certificate present: {certificate != null}");
            if (certificate != null)
            {
                // Disposed certificate inspection must not turn successful authentication into failure.
                try
                {
                    text.AppendLine($"Binding certificate thumbprint: {certificate.Thumbprint}");
                    text.AppendLine($"Binding certificate expires: {FormatUtc(certificate.NotAfter)}");
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    text.AppendLine("Binding certificate details unavailable");
                }
            }
            return text.ToString();
        }

        internal static string DescribeResult(AuthenticationResult result)
        {
            if (result == null) return "No authentication result";
            return $"TenantId: {result.TenantId}\nCorrelationId: {result.CorrelationId}\nTokenSource: {result.AuthenticationResultMetadata?.TokenSource.ToString() ?? "not supplied"}\n"
                + DescribeToken(result.ToAccessToken());
        }
    }
}
