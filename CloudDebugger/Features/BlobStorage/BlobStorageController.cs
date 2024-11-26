using Azure.MyIdentity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobStorage;

/// <summary>
/// This tool allows you to read and write blobs to an Azure Storage Account.
/// </summary>
public class BlobStorageController : Controller
{
    private readonly ILogger<BlobStorageController> _logger;
    private const string authenticationApproach = "authenticationApproach";

    public BlobStorageController(ILogger<BlobStorageController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }


    [HttpGet]
    public IActionResult AccessBlobs()
    {
        var model = new BlobStorageModel()
        {
            StorageAccountName = "",
            ContainerName = "",
            BlobName = "MyBlob.txt",
            AnonymousAccess = false
        };

        try
        {
            model.ContainerContent = TryGetContainerContent(model);
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }


        return View(model);
    }

    [HttpPost]
    public IActionResult AccessBlobs(BlobStorageModel model, string button)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new BlobStorageModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            switch (button)
            {
                case "listblobs":
                    //Do nothing, we always lists the files when we click on a button
                    break;
                case "loadblob":
                    _logger.LogInformation("BlobStorage.LoadBlob");
                    model.FileContent = "";
                    model.FileContent = LoadBlob(model);
                    break;
                case "writeblob":
                    _logger.LogInformation("BlobStorage.WriteBlob");
                    WriteBlob(model);
                    break;
                default:
                    break;
            }
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        try
        {
            //Always try to get the content, even if the above fails
            model.ContainerContent = TryGetContainerContent(model);
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        return View(model);
    }


    /// <summary>
    /// Try to get a list of all the blobs for a given container
    /// </summary>
    /// <param name="model"></param>
    /// <returns>Returns an empty list if it can't access the content</returns>
    private List<(string name, string size)> TryGetContainerContent(BlobStorageModel model)
    {
        var result = new List<(string name, string size)>();

        var containerName = model.ContainerName?.Trim() ?? "";

        if (!String.IsNullOrEmpty(containerName))
        {
            (var client, var message) = GetBlobServiceClient(model);
            ViewData[authenticationApproach] = message;

            if (client != null)
            {
                var container = client.GetBlobContainerClient(containerName);

                var blobs = container.GetBlobs().ToList();

                foreach (var blob in blobs)
                {
                    var blobSize = blob.Properties.ContentLength ?? 0;
                    result.Add((blob.Name, blobSize.ToString()));
                }
            }
        }

        return result;
    }


    private string? LoadBlob(BlobStorageModel model)
    {
        (var client, var message) = GetBlobServiceClient(model);
        ViewData[authenticationApproach] = message;

        if (client != null)
        {
            var container = client.GetBlobContainerClient(model.ContainerName);
            BlobClient blobClient = container.GetBlobClient(model.BlobName);

            BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;
            return downloadResult.Content.ToString();
        }
        else
        {
            return null;
        }
    }


    private void WriteBlob(BlobStorageModel model)
    {
        (var client, var message) = GetBlobServiceClient(model);
        ViewData[authenticationApproach] = message;

        if (client != null)
        {
            var container = client.GetBlobContainerClient(model.ContainerName);
            BlobClient blobClient = container.GetBlobClient(model.BlobName);

            blobClient.UploadAsync(BinaryData.FromString(model.FileContent ?? "Empty"), overwrite: true).Wait();
        }
    }


    /// <summary>
    /// Get a BlobServiceClient 
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
    private static (BlobServiceClient? client, string message) GetBlobServiceClient(BlobStorageModel model)
    {
        var storageAccountName = model.StorageAccountName?.ToLower().Trim() ?? "";
        var sasToken = model.SASToken?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(storageAccountName))
            return (null, "storageAccountName missing");

        var storageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

        var blobOptions = new BlobClientOptions()
        {
        };

        if (model.AnonymousAccess)
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
                var message = "Tried to authenticate using system-assigned managed identity";
                var client = new BlobServiceClient(storageUri, new MyDefaultAzureCredential(defaultCredentialOptions), blobOptions);
                return (client, message);
            }
            else
            {
                var message = $"Tried to authenticate using-assigned managed identity, ClientID={clientId}";
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
            else if (sasToken.Contains("sig="))
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
}
