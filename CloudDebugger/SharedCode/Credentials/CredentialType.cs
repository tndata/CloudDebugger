namespace CloudDebugger.SharedCode.Credentials;

/// <summary>
/// Credential selections used by the explorer and caching tools.
/// Keep these values stable: existing forms and URLs use the numeric IDs.
/// </summary>
public enum CredentialType
{
    None = 0,
    AuthorizationCodeCredential = 1,
    AzureCliCredential = 2,
    AzureDeveloperCliCredential = 3,
    AzurePowerShellCredential = 4,
    AzurePipelinesCredential = 5,
    ClientAssertionCredential = 6,
    ClientCertificateCredential = 7,
    ClientSecretCredential = 8,
    DefaultAzureCredential = 9,
    DeviceCodeCredential = 10,
    EnvironmentCredential = 11,
    InteractiveBrowserCredential = 12,
    ManagedIdentityCredential = 13,
    OnBehalfOfCredential = 14,
    SharedTokenCacheCredential = 15,
    UsernamePasswordCredential = 16,
    VisualStudioCodeCredential = 17,
    VisualStudioCredential = 18,
    WorkloadIdentityCredential = 19,
    BrokerCredential = 20,
}
