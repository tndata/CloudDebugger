using Azure;
using Azure.MyIdentity;
using Azure.Storage.Queues;

namespace CloudDebugger.SharedCode.QueueStorage;

/// <summary>
/// Shared Queue Storage Client Builder
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
    public static (QueueClient client, string message) CreateQueueClient(Uri queueUrl, string sasToken)
    {
        string message = "";
        if (string.IsNullOrWhiteSpace(sasToken))
        {
            // See https://github.com/microsoft/azure-container-apps/issues/442
            // because one or more UserAssignedIdentity can be assigned to an Azure Resource, we have to be explicit about which one to use.

            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";

            var defaultCredentialOptions = new DefaultAzureCredentialOptions();

            if (string.IsNullOrEmpty(clientId))
            {
                message = "Tried to authenticate using system-assigned managed identity";
            }
            else
            {
                message = $"Tried to authenticate using-assigned managed identity, ClientId={clientId}";
                defaultCredentialOptions.ManagedIdentityClientId = clientId;
            }

            var client = new QueueClient(queueUrl, new MyDefaultAzureCredential(defaultCredentialOptions));
            return (client, message);
        }
        else
        {
            //Authenticate using SAS Token
            message = "Tried to authenticate using Queue Service SAS URL";

            var credentials = new AzureSasCredential(sasToken);
            var client = new QueueClient(queueUrl, credentials);
            return (client, message);
        }
    }
}
