using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudDebugger.SharedCode.BlobStorage;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CloudDebugger.Features.BlobVersioning;

/// <summary>
/// BlobVersioning Tool
/// </summary>
public class BlobVersioningController : Controller
{
    private const string defaultBlobName = "VersionedBlob.txt";

    private const string storageAccountSessionKey = "BlobStorageAccount";
    private const string containerSessionKey = "BlobContainer";
    private const string sasTokenSessionKey = "BlobSasToken";
    private const string blobNameSessionKey = "BlobName";


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
    public async Task<IActionResult> Index(BlobVersioningModel model, string button, CancellationToken cancellationToken)
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
            (var client, var message) = BlobStorageClientBuilder.CreateBlobServiceClient(model.StorageAccountName, model.SASToken, anonymousAccess: false);
            model.AuthenticationMessage = message;

            if (client != null)
            {
                switch (button)
                {
                    case "createversionedblob":
                        await CreateVersionedBlobAsync(model, client, cancellationToken);
                        model.Message = "Blob created and overwritten 10 times.";
                        break;
                    case "readversionedblob":
                        model.BlobVersions = await ReadVersionedBlobAsync(model, client, cancellationToken);
                        model.Message = "Read all blob versions.";
                        break;
                    default:
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
    private static async Task<List<BlobVersionDetails>?> ReadVersionedBlobAsync(BlobVersioningModel model, BlobServiceClient client, CancellationToken cancellationToken)
    {
        var result = new List<BlobVersionDetails>();

        var container = client.GetBlobContainerClient(model?.ContainerName?.Trim() ?? "");

        var blobVersions = container.GetBlobsAsync(BlobTraits.None, BlobStates.Version | BlobStates.Deleted, prefix: model?.BlobName, cancellationToken: cancellationToken);

        await foreach (BlobItem version in blobVersions)
        {
            string blobContent = "";

            // Only download blob content for non-deleted blobs.
            if (!version.Deleted)
            {
                var blobClient = container.GetBlobClient(model?.BlobName).WithVersion(version.VersionId);
                BlobDownloadResult downloadResult = (await blobClient.DownloadContentAsync(cancellationToken)).Value;
                blobContent = downloadResult.Content.ToString();
            }
            else
            {
                blobContent = "Blob content can't be downloaded for deleted blobs.";
            }

            var blobDetail = new BlobVersionDetails()
            {
                Name = version.Name,
                VersionId = version.VersionId,
                Content = blobContent,
                IsLatestVersion = version.IsLatestVersion ?? false,
                IsDeleted = version.Deleted
            };

            result.Add(blobDetail);
        }

        //Sort the result
        result = result.OrderByDescending(r => r.IsLatestVersion)
                       .ThenByDescending(r => DateTime.ParseExact(r.VersionId ?? "", "yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture)).ToList();

        return result;
    }


    /// <summary>
    /// Create a blob and overwrite it 10 times
    /// </summary>
    /// <param name="model"></param>
    /// <param name="client"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private static async Task CreateVersionedBlobAsync(BlobVersioningModel model, BlobServiceClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.ContainerName))
            throw new ArgumentNullException(nameof(model), "model is null or empty.");

        var container = client.GetBlobContainerClient(model.ContainerName.Trim());

        for (int i = 1; i <= 10; i++)
        {
            BlobClient blobClient = container.GetBlobClient(model.BlobName);

            string blobContents = $"Sample blob data #{i}, written at " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            await Task.Delay(1000, cancellationToken);

            await blobClient.UploadAsync(BinaryData.FromString(blobContents), overwrite: true, cancellationToken: cancellationToken);
        }
    }
}
