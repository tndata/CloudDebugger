using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudDebugger.SharedCode.BlobStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp;

namespace CloudDebugger.Features.BlobStorageEditor;

/// <summary>
/// This tool allows you to read and write blobs to an Azure Storage Account.
/// </summary>
public class BlobStorageEditorController : Controller
{
    private readonly ILogger<BlobStorageEditorController> _logger;

    private const string storageAccountSessionKey = "blobStorageAccount";
    private const string containerSessionKey = "blobContainer";
    private const string sasTokenSessionKey = "blobSasToken";

    public BlobStorageEditorController(ILogger<BlobStorageEditorController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult AccessBlobs()
    {
        var model = new BlobStorageModel()
        {
            StorageAccountName = HttpContext.Session.GetString(storageAccountSessionKey),
            ContainerName = HttpContext.Session.GetString(containerSessionKey),
            SASToken = HttpContext.Session.GetString(sasTokenSessionKey),
            BlobName = "MyBlob.txt",
            AnonymousAccess = false
        };

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
        model.AuthenticationMessage = "";
        ModelState.Clear();

        string storageAccount = model.StorageAccountName?.Trim() ?? "";
        string containerName = model.ContainerName?.Trim() ?? "";
        string sasToken = model.SASToken?.Trim() ?? "";

        //Remember Storage Account and ContainerName
        HttpContext.Session.SetString(storageAccountSessionKey, storageAccount);
        HttpContext.Session.SetString(containerSessionKey, containerName);
        HttpContext.Session.SetString(sasTokenSessionKey, sasToken);

        BlobServiceClient? client = null;

        try
        {
            (client, var message) = BlobStorageClientBuilder.CreateBlobServiceClient(model.StorageAccountName, model.SASToken, model.AnonymousAccess);
            model.AuthenticationMessage = message;

            if (client != null)
            {
                switch (button)
                {
                    case "listblobs":
                        //Do nothing, we always lists the files when we click on a button
                        break;
                    case "loadblob":

                        _logger.LogInformation("BlobStorage.LoadBlob");
                        var (blobDetails, blobContent) = LoadBlob(model, client);
                        model.Blob = blobDetails;
                        model.BlobContent = blobContent;
                        break;
                    case "writeblob":
                        _logger.LogInformation("BlobStorage.WriteBlob");
                        WriteBlob(model, client);
                        break;
                    default:
                        break;
                }
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
            if (client != null)
                model.ContainerContent = TryGetContainerContent(model, client);
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
    private static List<(string name, string size)> TryGetContainerContent(BlobStorageModel model, BlobServiceClient client)
    {
        var result = new List<(string name, string size)>();

        var containerName = model.ContainerName?.Trim() ?? "";

        if (!String.IsNullOrEmpty(containerName))
        {
            var container = client.GetBlobContainerClient(containerName);

            if (model.HierarchicalNamespaceEnabled)
            {
                GetAllBlobsInHierachialBlogStorage(model, result, container);
            }
            else
            {
                GetBlogsInNormalStorageAccount(model, result, container);
            }
        }

        return result;
    }

    private static void GetBlogsInNormalStorageAccount(BlobStorageModel model, List<(string name, string size)> result, BlobContainerClient container)
    {
        var blobs = container.GetBlobs(traits: BlobTraits.Metadata, states: BlobStates.None, prefix: model.Path).ToList();

        // Always add an root element to the list, for easier navigation.
        result.Add(("/", ""));

        foreach (var blob in blobs)
        {
            // Is it a folder?
            if (blob.Metadata.TryGetValue("hdi_isfolder", out var isFolder) && isFolder == "true")
            {
                result.Add(("/" + blob.Name, ""));
            }
            else
            {
                var blobSize = blob.Properties.ContentLength ?? 0;
                result.Add((blob.Name, blobSize.ToString()));
            }
        }
    }

    private static void GetAllBlobsInHierachialBlogStorage(BlobStorageModel model, List<(string name, string size)> result, BlobContainerClient container)
    {
        var blobs = container.GetBlobsByHierarchy(traits: BlobTraits.Metadata, states: BlobStates.None, delimiter: "/", prefix: model.Path).ToList();

        result.Add(("/", ""));

        foreach (var blob in blobs)
        {
            // Is it a folder?
            if (blob.IsPrefix)
            {
                result.Add(("/" + blob.Prefix, ""));
            }
            else
            {
                var blobSize = blob.Blob.Properties.ContentLength ?? 0;
                result.Add((blob.Blob.Name, blobSize.ToString()));
            }
        }
    }

    private static (BlobDetails? blobDetails, string? blobContent) LoadBlob(BlobStorageModel model, BlobServiceClient client)
    {
        if (string.IsNullOrWhiteSpace(model.ContainerName))
            throw new ArgumentNullException(nameof(model), "model.ContainerName is null or empty.");
        if (string.IsNullOrWhiteSpace(model.BlobName))
            throw new ArgumentNullException(nameof(model), "model.BlobName is null or empty.");

        var container = client.GetBlobContainerClient(model.ContainerName.Trim());
        BlobClient blobClient = container.GetBlobClient(model.BlobName.Trim());

        BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;

        var lastAccessed = "[Not set]";
        if (downloadResult.Details.LastAccessed != DateTimeOffset.MinValue)
            lastAccessed = downloadResult.Details.LastAccessed.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

        var content = downloadResult.Content.ToString();

        var blobDetails = new BlobDetails()
        {
            BlobType = downloadResult.Details.BlobType.ToString(),
            ContentType = downloadResult.Details.ContentType.ToString(),
            CreatedOn = downloadResult.Details.CreatedOn.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            LastAccessed = lastAccessed,
            LastModified = downloadResult.Details.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
        };

        foreach (var metadata in downloadResult.Details.Metadata)
        {
            blobDetails.MetaData.Add(metadata.Key, metadata.Value.ToString());
        }

        return (blobDetails, content);
    }


    private static void WriteBlob(BlobStorageModel model, BlobServiceClient client)
    {
        if (model != null)
        {
            if (string.IsNullOrWhiteSpace(model.ContainerName))
                throw new ArgumentNullException(nameof(model), "model is null or empty.");
            if (string.IsNullOrWhiteSpace(model.BlobName))
                throw new ArgumentNullException(nameof(model), "model is null or empty.");

            var container = client.GetBlobContainerClient(model.ContainerName.Trim());
            BlobClient blobClient = container.GetBlobClient(model.BlobName.Trim());

            blobClient.UploadAsync(BinaryData.FromString(model.BlobContent ?? "Empty"), overwrite: true).Wait();
        }
    }
}
