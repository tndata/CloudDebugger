using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudDebugger.SharedCode.BlobStorage;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CloudDebugger.Features.BlobStorageAccessTiers;

/// <summary>
/// Blob access tier tool. 
///
/// This tool is used for exploring the blob storage access tiers.
/// </summary>
public class BlobStorageAccessTiersController : Controller
{
    private const string storageAccountSessionKey = "StorageAccount";
    private const string containerNameSessionKey = "blobContainerName";
    private const string sasTokenSessionKey = "blobSasToken";

    private const string HotTier = "Hot";
    private const string CoolTier = "Cool";
    private const string ColdTier = "Cold";
    private const string ArchiveTier = "Archive";

    public BlobStorageAccessTiersController()
    {
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new BlobStorageAccessTiersModel()
        {
            StorageAccountName = HttpContext.Session.GetString(storageAccountSessionKey) ?? "",
            ContainerName = HttpContext.Session.GetString(containerNameSessionKey) ?? "",
            SASToken = HttpContext.Session.GetString(sasTokenSessionKey) ?? "",
        };

        return View(model);
    }


    [HttpPost]
    public IActionResult Index(BlobStorageAccessTiersModel model, string button)
    {
        if (!ModelState.IsValid || model == null)
            return View(new BlobStorageAccessTiersModel());

        model.Message = "";
        model.ErrorMessage = "";
        model.AuthenticationMessage = "";
        ModelState.Clear();

        string storageAccount = model.StorageAccountName?.Trim() ?? "";
        string containerName = model.ContainerName?.Trim() ?? "";
        string sasToken = model.SASToken?.Trim() ?? "";

        //Remember these fields in the session
        HttpContext.Session.SetString(storageAccountSessionKey, storageAccount);
        HttpContext.Session.SetString(containerNameSessionKey, containerName);
        HttpContext.Session.SetString(sasTokenSessionKey, sasToken);

        try
        {
            (var client, var message) = BlobStorageClientBuilder.CreateBlobServiceClient(storageAccount,
                                                                                      sasToken,
                                                                                      anonymousAccess: false);
            model.AuthenticationMessage = message;

            string blobName = "";
            if (button.Contains(':'))
            {
                var parts = button.Split(':');
                button = parts[0];
                blobName = parts[1];
            }

            if (client != null)
            {
                switch (button)
                {
                    case "CreateBlobs":
                        CreateSampleBlobs(model, client);
                        model.Message = "Created sample blobs.";
                        break;
                    case "ListBlobs":
                        model.Blobs = ListAllBlobs(model, client);
                        model.Message = "Read all blobs.";
                        break;
                    case HotTier:
                    case CoolTier:
                    case ColdTier:
                    case ArchiveTier:

                        ChangeBlobTier(client, model.ContainerName, blobName, button);

                        model.Blobs = ListAllBlobs(model, client);

                        model.Message = $"Blob {blobName} moved to {button} tier.";
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
    /// Changes the access tier for a given blob.
    /// </summary>
    /// <param name="client">BlobServiceClient instance to interact with the storage account.</param>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to modify.</param>
    /// <param name="tier">The desired access tier ("Hot", "Cool", "Cold", or "Archive").</param>
    /// <exception cref="ArgumentException">Thrown if any parameter is missing or invalid.</exception>
    /// <exception cref="RequestFailedException">Thrown if the operation fails (e.g., blob not found).</exception>
    private static void ChangeBlobTier(BlobServiceClient client, string? containerName, string? blobName, string tier)
    {
        if (string.IsNullOrEmpty(containerName))
            throw new ArgumentException("Container name is missing.", nameof(containerName));

        if (string.IsNullOrEmpty(blobName))
            throw new ArgumentException("Blob name is missing.", nameof(blobName));

        if (string.IsNullOrEmpty(tier))
            throw new ArgumentException("Tier is missing.", nameof(tier));

        var container = client.GetBlobContainerClient(containerName); // Replace with your container name
        var blobClient = container.GetBlobClient(blobName);

        AccessTier accessTier = tier switch
        {
            HotTier => AccessTier.Hot,
            CoolTier => AccessTier.Cool,
            ColdTier => AccessTier.Cold,
            ArchiveTier => AccessTier.Archive,
            _ => throw new ArgumentException($"Invalid tier: {tier}", nameof(tier))
        };

        blobClient.SetAccessTier(accessTier);
    }


    /// <summary>
    /// Return a list of all the blobs and their access tier data
    /// </summary>
    /// <param name="model"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private static List<BlobDetails> ListAllBlobs(BlobStorageAccessTiersModel model, BlobServiceClient client)
    {
        if (string.IsNullOrEmpty(model.ContainerName))
            throw new ArgumentException("Container name is missing.", nameof(model));

        var result = new List<BlobDetails>();

        string containerName = model.ContainerName.Trim();

        var container = client.GetBlobContainerClient(containerName);

        var blobs = container.GetBlobs(new GetBlobsOptions
        {
            Traits = BlobTraits.None,
            States = BlobStates.None
        });

        foreach (BlobItem blob in blobs)
        {
            string blobContent = "";

            if (blob.Properties.AccessTier != ArchiveTier)
            {
                var blobClient = container.GetBlobClient(blob.Name);
                BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;
                blobContent = downloadResult.Content.ToString();
            }
            else
            {
                blobContent = "Blob is archived, we can't read archived blobs.";
            }

            var blobDetail = new BlobDetails()
            {
                Name = blob.Name,
                AccessTier = blob.Properties.AccessTier?.ToString() ?? "[Unknown]",
                AccessTierInferred = blob.Properties.AccessTierInferred.ToString(),
                AccessTierChangedOn = blob.Properties.AccessTierChangedOn?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                RehydratePriority = blob.Properties.RehydratePriority?.ToString() ?? "[Unknown]",
                ArchiveStatus = blob.Properties.ArchiveStatus?.ToString() ?? "Null (No rehydration pending)",
                Content = blobContent,
            };

            blobDetail.TierOrder = blobDetail.AccessTier switch
            {
                HotTier => 1,
                CoolTier => 2,
                ColdTier => 3,
                ArchiveTier => 4,
                _ => 1
            };

            blobDetail.AccessTierInferred = blobDetail.AccessTierInferred switch
            {
                "True" => "True (default storage account tier)",
                _ => "False (Set explicitly)"
            };

            result.Add(blobDetail);
        }

        //Sort and return the result
        return result.OrderBy(r => r.TierOrder)
                       .ThenBy(r => r.Name).ToList();
    }

    /// <summary>
    /// Create five sample blobs
    /// </summary>
    /// <param name="model"></param>
    /// <param name="client"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private static void CreateSampleBlobs(BlobStorageAccessTiersModel model, BlobServiceClient client)
    {
        if (string.IsNullOrWhiteSpace(model.ContainerName))
            throw new ArgumentNullException(nameof(model), "model.ContainerName is null or empty.");

        var container = client.GetBlobContainerClient(model.ContainerName.Trim());

        for (int i = 1; i <= 5; i++)
        {
            string blobNname = $"AccessTierBlob{i}.txt";

            BlobClient blobClient = container.GetBlobClient(blobNname);

            string blobContents = $"Sample blob data #{i}, written at " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            blobClient.UploadAsync(BinaryData.FromString(blobContents), overwrite: true).Wait();
        }
    }
}
