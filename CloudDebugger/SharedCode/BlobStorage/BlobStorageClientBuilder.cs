using Azure.MyIdentity;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace CloudDebugger.SharedCode.BlobStorage;

/// <summary>
/// Shared Queue Storage Client Builder
/// </summary>
public static class BlobStorageClientBuilder
{
    /// <summary>
    /// Create an instance of the BlobServiceClient 
    /// Supports:
    /// * Connection string     "BlobEndpoint=https://clouddebuggerstorage.blob.core.windows.net/;..."
    /// * SAS token             "sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-08-21T21:31:22Z&st=2..."
    /// * Blob service SAS URL  "https://clouddebuggerstorage.blob.core.windows.net/?sv=2022-11-02&ss=bf..."
    /// * Access token          "8v9oGniVMKa2Oy495obZ6qc/+xz+mX0bIPPVO65sO1SKOe9b1MrOvpyRJXJzvdWNAT8b/5IQ1z..."
    /// * Managed Identity
    /// 
    /// Resources:
    /// https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string
    /// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started?tabs=sas-token
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public static (BlobServiceClient? client, string message) CreateBlobServiceClient(string? storageAccountName, string? sasToken, bool anonymousAccess)
    {
        storageAccountName = storageAccountName?.ToLower().Trim() ?? "";
        sasToken = sasToken?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(storageAccountName))
            return (null, "storageAccountName missing");

        var storageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

        var blobOptions = new BlobClientOptions()
        {
            //For future use
        };

        if (anonymousAccess)
        {
            var message = "Using anonymous access";
            var client = new BlobServiceClient(storageUri, blobOptions);
            return (client, message);
        }

        if (string.IsNullOrEmpty(sasToken))
        {
            //Try using managed identity
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";

            var defaultCredentialOptions = new DefaultAzureCredentialOptions();

            if (string.IsNullOrEmpty(clientId))
            {
                var message = "Tried to authenticate using DefaultAzureCredential";
                var client = new BlobServiceClient(storageUri, new MyDefaultAzureCredential(defaultCredentialOptions), blobOptions);
                return (client, message);
            }
            else
            {
                var message = $"Tried to authenticate using DefaultAzureCredential, ClientID={clientId}";
                defaultCredentialOptions.ManagedIdentityClientId = clientId;
                var client = new BlobServiceClient(storageUri, new MyDefaultAzureCredential(defaultCredentialOptions), blobOptions);
                return (client, message);
            }
        }
        else
        {
            if (sasToken.StartsWith("BlobEndpoint"))
            {
                // SAS Connection string
                var message = "Tried to authenticate using SAS Connection string";
                var client = new BlobServiceClient(sasToken.ToString(), blobOptions);
                return (client, message);
            }
            else if (sasToken.StartsWith("http"))
            {
                // SAS Connection string
                var message = "Tried to authenticate using SAS Connection string";
                var client = new BlobServiceClient(new Uri(sasToken), blobOptions);
                return (client, message);
            }
            else if (IsSasToken(sasToken))
            {
                // SAS token
                var message = "Tried to authenticate using SAS token";
                var url = new Uri($"{storageUri}?{sasToken}");
                var client = new BlobServiceClient(url, blobOptions);

                return (client, message);
            }
            else
            {
                // Account access key
                var message = "Tried to authenticate using account access key";
                var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, sasToken);
                var client = new BlobServiceClient(storageUri, sharedKeyCredential, blobOptions);
                return (client, message);
            }
        }
    }

    /// <summary>
    /// Returns true if the string is a SAS token
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    static bool IsSasToken(string token)
    {
        return token.Contains("sv=") && token.Contains("se=") && token.Contains("sig=");
    }
}
