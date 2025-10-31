using Azure.MyIdentity;

namespace CloudDebugger.SharedCode.Credentials;

/// <summary>
/// Provides factory methods for creating various Azure authentication credential instances based on a specified
/// credential type identifier.
/// </summary>

public static class CredentialsFactory
{
    /// <summary>
    /// Create the TokenCredential instance based on the selected credential Id.
    /// </summary>
    /// <param name="credentialId">The credential type ID</param>
    /// <param name="clientId">Optional client ID for credentials that support it</param>
    /// <returns>CredentialResult containing the credential and any messages</returns>
    public static CredentialResult? CreateTokenCredentialInstance(int credentialId, string? clientId = null)
    {
        CredentialResult? result = credentialId switch
        {
            1 => CreateAuthorizationCodeCredential(),
            2 => CreateAzureCliCredential(),
            3 => CreateAzureDeveloperCliCredential(),
            4 => CreateAzurePowerShellCredential(),
            5 => CreateAzurePipelinesCredential(),
            6 => CreateClientAssertionCredential(),
            7 => CreateClientCertificateCredential(),
            8 => CreateClientSecretCredential(),
            9 => CreateDefaultAzureCredential(clientId),
            10 => CreateDeviceCodeCredential(),
            11 => CreateEnvironmentCredential(),
            12 => CreateInteractiveBrowserCredential(),
            13 => CreateManagedIdentityCredential(clientId),
            14 => CreateOnBehalfOfCredential(),
            15 => CreateSharedTokenCacheCredential(),
            16 => CreateUsernamePasswordCredential(),
            17 => CreateVisualStudioCodeCredential(),
            18 => CreateVisualStudioCredential(),
            19 => CreateWorkloadIdentityCredential(),
            20 => CreateBrokerCredential(),
            _ => null
        };

        return result;
    }

    /// <summary>
    /// Configures standard diagnostic options for credentials
    /// </summary>
    private static void ConfigureDiagnostics(dynamic diagnostics)
    {
        diagnostics.IsLoggingEnabled = true;
        diagnostics.LoggedHeaderNames.Add("*");
        diagnostics.LoggedQueryParameters.Add("*");
        diagnostics.IsAccountIdentifierLoggingEnabled = true;
        diagnostics.IsLoggingContentEnabled = true;
        diagnostics.IsDistributedTracingEnabled = true;
        diagnostics.IsTelemetryEnabled = true;
    }

    private static CredentialResult CreateAuthorizationCodeCredential()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Enables authentication to Microsoft Entra ID using Azure CLI to obtain an access token.
    /// </summary>
    private static CredentialResult CreateAzureCliCredential()
    {
        var options = new AzureCliCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new AzureCliCredential(options);
        return new() { Credential = credential, Message = "Requires 'az login' to be completed first" };
    }

    private static CredentialResult CreateAzureDeveloperCliCredential()
    {
        var options = new AzureDeveloperCliCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new AzureDeveloperCliCredential(options);
        return new() { Credential = credential, Message = "Requires 'azd auth login' to be completed first" };
    }

    private static CredentialResult CreateAzurePowerShellCredential()
    {
        var options = new AzurePowerShellCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new AzurePowerShellCredential(options);
        return new() { Credential = credential, Message = "Requires 'Connect-AzAccount' to be completed first" };
    }

    private static CredentialResult CreateAzurePipelinesCredential()
    {
        // Get required parameters from environment variables
        string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "";
        string clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";
        string serviceConnectionId = Environment.GetEnvironmentVariable("AZURESUBSCRIPTION_SERVICE_CONNECTION_ID") ?? "";
        string systemAccessToken = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN") ?? "";

        // Validate required parameters
        if (string.IsNullOrEmpty(tenantId) ||
            string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(serviceConnectionId) ||
            string.IsNullOrEmpty(systemAccessToken))
        {
            var missing = new List<string>();
            if (string.IsNullOrEmpty(tenantId)) missing.Add("AZURE_TENANT_ID");
            if (string.IsNullOrEmpty(clientId)) missing.Add("AZURE_CLIENT_ID");
            if (string.IsNullOrEmpty(serviceConnectionId)) missing.Add("AZURESUBSCRIPTION_SERVICE_CONNECTION_ID");
            if (string.IsNullOrEmpty(systemAccessToken)) missing.Add("SYSTEM_ACCESSTOKEN");

            return new()
            {
                Credential = null,
                Message = $"AzurePipelinesCredential unavailable - missing these environment variables: {string.Join(", ", missing)}"
            };
        }

        var options = new AzurePipelinesCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new AzurePipelinesCredential(tenantId, clientId, serviceConnectionId, systemAccessToken, options);
        return new() { Credential = credential, Message = "Works only within Azure DevOps Pipelines" };
    }

    private static CredentialResult CreateClientAssertionCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateClientCertificateCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateClientSecretCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateDefaultAzureCredential(string? clientId)
    {
        var options = new DefaultAzureCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        if (!string.IsNullOrEmpty(clientId))
        {
            options.ManagedIdentityClientId = clientId;
        }

        var credential = new DefaultAzureCredential(options);
        return new() { Credential = credential, Message = "Tries multiple authentication methods in sequence" };
    }

    private static CredentialResult CreateDeviceCodeCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateEnvironmentCredential()
    {
        var options = new EnvironmentCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new EnvironmentCredential(options);
        return new() { Credential = credential, Message = "Requires environment variables: AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET (or AZURE_CLIENT_CERTIFICATE_PATH)" };
    }

    /// <summary>
    /// This one only works for non-server side applications (desktop, console, mobile....)
    /// </summary>
    private static CredentialResult CreateInteractiveBrowserCredential()
    {
        var options = new InteractiveBrowserCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new InteractiveBrowserCredential(options);
        return new() { Credential = credential, Message = "Opens browser for interactive authentication - works only for desktop/console apps" };
    }
    private static CredentialResult CreateManagedIdentityCredential(string? clientId)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            // User-assigned managed identity  
            var options = new TokenCredentialOptions();
            ConfigureDiagnostics(options.Diagnostics);
            var credential = new ManagedIdentityCredential(clientId, options);
            return new() { Credential = credential, Message = $"User-assigned managed identity with Client ID: {clientId}" };
        }
        else
        {
            //System-assigned managed identity
            var credential = new ManagedIdentityCredential();
            return new() { Credential = credential, Message = "System-assigned managed identity - set AZURE_CLIENT_ID environment variable for user-assigned" };
        }
    }

    private static CredentialResult CreateOnBehalfOfCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateSharedTokenCacheCredential()
    {
        // This mechanism for Visual Studio authentication has been replaced by the VisualStudioCredential.
        throw new NotImplementedException();
    }

    private static CredentialResult CreateUsernamePasswordCredential()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.usernamepasswordcredential?view=azure-dotnet
        throw new NotImplementedException();
    }

    private static CredentialResult CreateVisualStudioCodeCredential()
    {
        var options = new VisualStudioCodeCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new VisualStudioCodeCredential(options);
        return new() { Credential = credential, Message = "Uses authentication from Visual Studio Code Azure Account extension" };
    }

    private static CredentialResult CreateVisualStudioCredential()
    {
        var options = new VisualStudioCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new VisualStudioCredential(options);
        return new() { Credential = credential, Message = "Uses authentication from Visual Studio IDE" };
    }

    private static CredentialResult CreateWorkloadIdentityCredential()
    {
        var options = new WorkloadIdentityCredentialOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new WorkloadIdentityCredential(options);
        return new() { Credential = credential, Message = "Supports Microsoft Entra Workload ID authentication on Kubernetes and other hosts" };
    }

    private static CredentialResult CreateBrokerCredential()
    {
        var options = new DevelopmentBrokerOptions();
        ConfigureDiagnostics(options.Diagnostics);

        var credential = new BrokerCredential(options);
        return new() { Credential = credential, Message = "Uses system authentication broker (WAM on Windows). Aimed for desktop computers with UI. Does not work in Azure." };
    }
}