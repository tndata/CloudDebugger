using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudDebugger.SharedCode.BlobStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobVersioning;

/// <summary>
/// BlobVersioning Tool
/// </summary>
public class BlobVersioningController : Controller
{
    private const string defaultBlobName = "VersionedBlob.txt";

    private const string storageAccountSessionKey = "blobStorageAccount";
    private const string containerSessionKey = "blobContainer";
    private const string sasTokenSessionKey = "blobSasToken";
    private const string blobNameSessionKey = "blobName";


    public BlobVersioningController()
    {
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new BlobVersioningModel()
        {
            StorageAccountName = HttpContext.Session.GetString(storageAccountSessionKey) ?? "",
            ContainerName = HttpContext.Session.GetString(containerSessionKey) ?? "",
            SASToken = HttpContext.Session.GetString(sasTokenSessionKey) ?? "",
            BlobName = HttpContext.Session.GetString(blobNameSessionKey) ?? defaultBlobName
        };

        return View(model);
    }


    [HttpPost]
    public IActionResult Index(BlobVersioningModel model, string button)
    {
        if (!ModelState.IsValid || model == null)
            return View(new BlobVersioningModel());

        model.Message = "";
        model.ErrorMessage = "";
        model.AuthenticationMessage = "";
        ModelState.Clear();

        string storageAccount = model.StorageAccountName?.Trim() ?? "";
        string containerName = model.ContainerName?.Trim() ?? "";
        string sasToken = model.SASToken?.Trim() ?? "";
        string blobName = model.BlobName?.Trim() ?? defaultBlobName;

        //Remember these fields in the session
        HttpContext.Session.SetString(storageAccountSessionKey, storageAccount);
        HttpContext.Session.SetString(containerSessionKey, containerName);
        HttpContext.Session.SetString(sasTokenSessionKey, sasToken);
        HttpContext.Session.SetString(blobNameSessionKey, blobName);

        try
        {
            (var client, var message) = BlobStorageClientBuilder.GetBlobServiceClient(model.StorageAccountName, model.SASToken, anonymousAccess: false);
            model.AuthenticationMessage = message;

            if (client != null)
            {
                switch (button)
                {
                    case "createversionedblob":
                        CreateVersionedBlob(model, client);
                        model.Message = "Blob created and overwritten 10 times.";
                        break;
                    case "readversionedblob":
                        model.BlobVersions = ReadVersionedBlob(model, client);
                        model.Message = "Read all blob versions.";
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

        return View(model);
    }


    /// <summary>
    /// Return a list of all the versions of a given blob.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private static List<BlobVersionDetails>? ReadVersionedBlob(BlobVersioningModel model, BlobServiceClient client)
    {
        var result = new List<BlobVersionDetails>();

        var container = client.GetBlobContainerClient(model?.ContainerName?.Trim() ?? "");

        var blobVersions = container.GetBlobs(BlobTraits.None, BlobStates.Version, prefix: model?.BlobName);

        foreach (BlobItem version in blobVersions)
        {
            //Download blob version
            var blobClient = container.GetBlobClient(model?.BlobName).WithVersion(version.VersionId);
            BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;
            string blobContents = downloadResult.Content.ToString();

            var blobDetail = new BlobVersionDetails()
            {
                Name = version.Name,
                VersionId = version.VersionId,
                Content = blobContents,
                IsLatestVersion = version.IsLatestVersion ?? false,
                IsDeleted = version.Deleted
            };

            result.Add(blobDetail);
        }

        //Sort the result
        result = result.OrderByDescending(r => r.IsLatestVersion)
                       .ThenByDescending(r => DateTime.Parse(r.VersionId ?? "")).ToList();

        return result;
    }


    /// <summary>
    /// Create a blob and overwrite it 10 times
    /// </summary>
    /// <param name="model"></param>
    /// <param name="client"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private static void CreateVersionedBlob(BlobVersioningModel model, BlobServiceClient client)
    {
        if (string.IsNullOrWhiteSpace(model.ContainerName))
            throw new ArgumentNullException(nameof(model), "model is null or empty.");

        var container = client.GetBlobContainerClient(model.ContainerName.Trim());

        for (int i = 1; i <= 10; i++)
        {
            BlobClient blobClient = container.GetBlobClient(model.BlobName);

            string blobContents = $"Sample blob data #{i}, written at " + DateTime.UtcNow;

            Thread.Sleep(1000);

            blobClient.UploadAsync(BinaryData.FromString(blobContents), overwrite: true).Wait();
        }
    }
}
