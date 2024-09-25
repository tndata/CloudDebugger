using Azure.MyIdentity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudDebugger.Features.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobStorage;

/// <summary>
/// This tool allows you to read and write blobs to an Azure Storage Account.
/// </summary>
public class BlobStorageController : Controller
{
    private readonly ILogger<BlobStorageController> _logger;

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
            StorageAccountName = "clouddebuggerstorage",
            ContainerName = "clouddebugger",
            BlobName = "MyBlob.txt"
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
            return View(new ReadWriteFilesModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            switch (button)
            {
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
        var containerName = model.ContainerName?.Trim() ?? "";

        var client = GetBlobServiceClient(model);

        var container = client.GetBlobContainerClient(containerName);

        var blobs = container.GetBlobs().ToList();

        var result = new List<(string name, string size)>();

        foreach (var blob in blobs)
        {
            var blobSize = blob.Properties.ContentLength ?? 0;
            result.Add((blob.Name, blobSize.ToString()));
        }

        return result;
    }


    private string? LoadBlob(BlobStorageModel model)
    {
        var client = GetBlobServiceClient(model);

        var container = client.GetBlobContainerClient(model.ContainerName);
        BlobClient blobClient = container.GetBlobClient(model.BlobName);

        BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;
        return downloadResult.Content.ToString();
    }


    private void WriteBlob(BlobStorageModel model)
    {
        var client = GetBlobServiceClient(model);

        var container = client.GetBlobContainerClient(model.ContainerName);
        BlobClient blobClient = container.GetBlobClient(model.BlobName);

        blobClient.UploadAsync(BinaryData.FromString(model.FileContent ?? "Empty"), overwrite: true).Wait();
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
    private BlobServiceClient GetBlobServiceClient(BlobStorageModel model)
    {
        var storageAccountName = model.StorageAccountName?.ToLower().Trim() ?? "";
        var sasToken = model.SASToken?.Trim() ?? "";

        var storageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

        var blobOptions = new BlobClientOptions()
        {
        };

        if (string.IsNullOrEmpty(sasToken))
        {
            //Use managed identity
            ViewData["authenticationApproach"] = "Tried to authenticate using managed identity";
            var credentials = new MyDefaultAzureCredential();
            return new BlobServiceClient(storageUri, credentials, blobOptions);
        }
        else
        {
            if (sasToken.StartsWith("BlobEndpoint"))
            {
                // SAS Connection string
                ViewData["authenticationApproach"] = "Tried to authenticate using SAS Connection string";
                return new BlobServiceClient(sasToken.ToString(), blobOptions);
            }
            else if (sasToken.StartsWith("http"))
            {
                // SAS Connection string
                ViewData["authenticationApproach"] = "Tried to authenticate using SAS Connection string";
                return new BlobServiceClient(new Uri(sasToken), blobOptions);
            }
            else if (sasToken.Contains("sig="))
            {
                // SAS token
                ViewData["authenticationApproach"] = "Tried to authenticate using SAS token";
                var url = new Uri($"{storageUri}?{sasToken}");
                return new BlobServiceClient(url, blobOptions);
            }
            else
            {
                // Account access key
                ViewData["authenticationApproach"] = "Tried to authenticate using account access key";
                var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, sasToken);
                return new BlobServiceClient(storageUri, sharedKeyCredential, blobOptions);
            }
        }
    }
}
