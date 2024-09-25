using Azure.MyIdentity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobStorageDelegationSASToken;

/// <summary>
/// Create a user delegation SAS token
/// ==================================
/// 
/// To get the User delegation key demo to work
/// 1. Create a storage account named clouddebuggerstorage
/// 2. In it, create a blog container named clouddebugger
/// 3. In it, upload a blob named MyBlob.txt
/// 4. On the storage account. Assign the role "Storage Blob Data Reader" to the identity of this tool.
/// 
/// Resources:
/// https://learn.microsoft.com/en-us/azure/storage/common/storage-account-sas-create-dotnet
/// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet
/// https://azure.microsoft.com/fr-fr/blog/announcing-user-delegation-sas-tokens-preview-for-azure-storage-blobs/ 
/// </summary>
public class BlobStorageDelegationSasTokenController : Controller
{
    private readonly ILogger<BlobStorageDelegationSasTokenController> _logger;

    public BlobStorageDelegationSasTokenController(ILogger<BlobStorageDelegationSasTokenController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new UserDelegationModel()
        {
            StorageAccountName = "clouddebuggerstorage",
            ContainerName = "clouddebugger",
            BlobName = "MyBlob.txt"
        };

        return View(model);
    }


    [HttpPost]
    public IActionResult Index(UserDelegationModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var storageAccountName = model.StorageAccountName?.ToLower().Trim().SanitizeInput() ?? "";
        var containerName = model.ContainerName?.ToLower().Trim().SanitizeInput() ?? "";
        string blobName = model.BlobName?.Trim() ?? "";

        try
        {
            var blobStorageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

            // Step #1, get an authentication token from Entra ID
            var credentials = new MyDefaultAzureCredential();

            // Step #2, get a BlobServiceClient with these credentials
            var client = new BlobServiceClient(blobStorageUri, credentials);

            // Step #3, get a user delegation key from Entra ID
            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-1); // To avoid clock skew issues
            var expiresOn = startsOn.AddDays(1);                 // Max 7 days

            var userDelegationKey = client.GetUserDelegationKey(startsOn, expiresOn);
            model.DelegationKey = userDelegationKey;

            // Step #4, Define the permissions for the SAS token
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Step #5, Specify the necessary permissions
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Step #6, Build the SAS token
            model.SASToken = sasBuilder.ToSasQueryParameters(userDelegationKey, storageAccountName).ToString();

            model.BlobUrl = $"https://{storageAccountName}.blob.core.windows.net/{containerName}/{blobName}";
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
            _logger.LogError(exc, "An error occurred while generating the SAS token for storage account {StorageAccountName}, container {ContainerName}, and blob {BlobName}.",
                                    storageAccountName,
                                    containerName,
                                    blobName.SanitizeInput());
        }

        return View(model);
    }
}
