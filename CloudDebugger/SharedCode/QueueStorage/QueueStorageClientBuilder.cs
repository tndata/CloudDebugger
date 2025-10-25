using Azure.MyIdentity;
using Azure.Storage;
using Azure.Storage.Queues;

namespace CloudDebugger.SharedCode.QueueStorage;

/// <summary>
/// Shared Queue Storage QueueServiceClient Builder
/// 
/// To create a CreateQueueServiceClient, it needs the name of the storage account and then one of the following:
/// 
/// * Storage account access key
/// * Storage account connection string
///   String that starts with BlobEndpoint=https://mystorageaccount12322.blob.core.windows.net/;QueueEndpoint=https://mystorageaccount12322.queue.core.windows.net/;FileEndpoint=https://mystorageaccount12322.file.core.windows.net/;TableEndpoint=https://mystorageaccount12322.table.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-12-11T20:02:33Z&st=2024-12-11T12:02:33Z&spr=https&sig=IBvTtzj3Pf1CJxtdS0Tjlj9JpF6Pn2QjFGlbQ4covDY%3D
/// * SAS Token
///   like "sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-12-11T20:02:33Z&st=2024-12-11T12:02:33Z&spr=https&sig=IBvTtzj3Pf1CJxtdS0Tjlj9JpF6Pn2QjFGlbQ4covDY%3D" 
/// * Queue SAS URL
///   like "https://mystorageaccount12322.queue.core.windows.net/?sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-12-11T20:02:33Z&st=2024-12-11T12:02:33Z&spr=https&sig=IBvTtzj3Pf1CJxtdS0Tjlj9JpF6Pn2QjFGlbQ4covDY%3D..."
/// </summary>
public static class QueueStorageClientBuilder
{
    /// <summary>
    /// Create an instance of the Queue Storage client
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="queueUrl"></param>
    /// <param name="queueName"></param>
    /// <returns></returns>
    public static (QueueServiceClient? client, string message) CreateQueueServiceClient(string? storageAccountName, string? sasToken)
    {
        storageAccountName = storageAccountName?.ToLower().Trim() ?? "";
        sasToken = sasToken?.Trim() ?? "";
        string message;

        if (string.IsNullOrWhiteSpace(storageAccountName))
            return (null, "storageAccountName missing");

        var storageUri = new Uri($"https://{storageAccountName}.queue.core.windows.net");

        var queueOptions = new QueueClientOptions()
        {

        };

        if (string.IsNullOrWhiteSpace(sasToken))
        {
            //Try using managed identity

            // See https://github.com/microsoft/azure-container-apps/issues/442
            // because one or more UserAssignedIdentity can be assigned to an Azure Resource, we have to be explicit about which one to use.

            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";

            var defaultCredentialOptions = new DefaultAzureCredentialOptions();

            if (string.IsNullOrEmpty(clientId))
            {
                message = "Tried to authenticate using system-assigned managed identity";
                var client = new QueueServiceClient(storageUri, new DefaultAzureCredential(defaultCredentialOptions), queueOptions);
                return (client, message);
            }
            else
            {
                message = $"Tried to authenticate using-assigned managed identity, ClientID={clientId}";
                defaultCredentialOptions.ManagedIdentityClientId = clientId;
                var client = new QueueServiceClient(storageUri, new DefaultAzureCredential(defaultCredentialOptions), queueOptions);
                return (client, message);
            }
        }
        else
        {
            if (sasToken.StartsWith("BlobEndpoint"))
            {
                // SAS Connection string
                message = "Tried to authenticate using SAS Connection string";
                var client = new QueueServiceClient(sasToken.ToString(), queueOptions);
                return (client, message);
            }
            else if (sasToken.StartsWith("http"))
            {
                // SAS Connection string
                message = "Tried to authenticate using SAS Connection string";
                var client = new QueueServiceClient(new Uri(sasToken), queueOptions);
                return (client, message);
            }
            else if (IsSasToken(sasToken))
            {
                // SAS token
                message = "Tried to authenticate using SAS token";
                var url = new Uri($"{storageUri}?{sasToken}");
                var client = new QueueServiceClient(url, queueOptions);

                return (client, message);
            }
            else
            {
                // Account access key
                message = "Tried to authenticate using storage account access key";
                var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, sasToken);
                var client = new QueueServiceClient(storageUri, sharedKeyCredential, queueOptions);
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
