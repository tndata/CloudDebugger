using Azure.MyIdentity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CloudDebugger.Features.BlobStorage;
using CloudDebugger.Features.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.HomePage
{
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
        public IActionResult GetUserDelegationSASToken()
        {
            var model = new UserDelegationModel();
            return View(model);
        }

        /// <summary>
        /// Resources:
        /// https://learn.microsoft.com/en-us/azure/storage/common/storage-account-sas-create-dotnet
        /// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet
        /// https://azure.microsoft.com/fr-fr/blog/announcing-user-delegation-sas-tokens-preview-for-azure-storage-blobs/ 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public IActionResult GetUserDelegationSASToken(UserDelegationModel model, string button)
        {
            if (model == null)
                return View(new UserDelegationModel());

            model.Message = "";
            model.ErrorMessage = "";
            model.SASToken = "";
            model.DelegationKey = null;
            ModelState.Clear();


            try
            {
                var blobStorageUri = $"https://{model.StorageAccountName}.blob.core.windows.net";

                // Step #1, get an authentication token from Entra ID
                var credentials = new MyDefaultAzureCredential();

                // Step #2, get a BlobServiceClient with these credentials
                var client = new BlobServiceClient(new Uri(blobStorageUri), credentials);

                // Step #3, get a user delegation key from Entra ID
                var startsOn = DateTimeOffset.UtcNow.AddMinutes(-1); // To avoid clock skew issues
                var expiresOn = startsOn.AddDays(1);                 // Max 7 days

                var userDelegationKey = client.GetUserDelegationKey(startsOn, expiresOn);
                model.DelegationKey = userDelegationKey;

                // Step #4, Define the permissions for the SAS token
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = model.ContainerName,
                    BlobName = model.BlobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                // Step #5, Specify the necessary permissions
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                //// Alternatively, you can combine multiple permissions
                //sasBuilder.SetPermissions(BlobContainerSasPermissions.Read |
                //                          BlobContainerSasPermissions.Write |
                //                          BlobContainerSasPermissions.List |
                //                          BlobContainerSasPermissions.Delete);

                // Step #6, Build the SAS token
                model.SASToken = sasBuilder.ToSasQueryParameters(userDelegationKey, model.StorageAccountName).ToString();

            }
            catch (Exception exc)
            {
                string str = $"Exception:\r\n{exc.Message}";
                model.ErrorMessage = str;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessBlobs()
        {
            var model = new BlobStorageModel();

            model.ContainerContent = TryGetContainerContent(model);
            return View(model);
        }

        [HttpPost]
        public IActionResult AccessBlobs(BlobStorageModel model, string button)
        {
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
                        model.FileContent = "";
                        model.FileContent = LoadBlob(model);
                        break;
                    case "writeblob":
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

                model.ContainerContent = TryGetContainerContent(model);
            }

            return View(model);
        }

        private List<(string name, string size)> TryGetContainerContent(BlobStorageModel model)
        {
            try
            {
                var client = GetBlobServiceClient(model);

                var container = client.GetBlobContainerClient(model.ContainerName);

                var blobs = container.GetBlobs().ToList();

                var result = new List<(string name, string size)>();

                foreach (var blob in blobs)
                {
                    var blobSize = blob.Properties.ContentLength ?? 0;
                    result.Add((blob.Name, blobSize.ToString()));
                }

                return result;

            }
            catch (Exception)
            {
                //Do nothing...
                return new List<(string name, string size)>();
            }
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

            blobClient.UploadAsync(BinaryData.FromString(model?.FileContent ?? "Empty"), overwrite: true).Wait();
        }


        /// <summary>
        /// Get a BlobServiceClient 
        /// 
        /// Supports:
        /// * Connection string     "BlobEndpoint=https://clouddebuggerstorage.blob.core.windows.net/;..."
        /// * SAS token             "sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-08-21T21:31:22Z&st=2..."
        /// * Blob service SAS URL  "https://clouddebuggerstorage.blob.core.windows.net/?sv=2022-11-02&ss=bf..."
        /// * Access token          "8v9oGniVMKa2Oy495obZ6qc/+xz+mX0bIPPVO65sO1SKOe9b1MrOvpyRJXJzvdWNAT8b/5IQ1z..."
        /// * Managed Identity
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private BlobServiceClient GetBlobServiceClient(BlobStorageModel model)
        {
            //https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string
            //https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started?tabs=sas-token

            var options = new BlobClientOptions()
            {
                //TODO: 
            };

            var storageUri = new Uri($"https://{model.StorageAccountName}.blob.core.windows.net");

            if (string.IsNullOrEmpty(model.SASToken))
            {
                //Use managed identity
                var credentials = new MyDefaultAzureCredential();
                return new BlobServiceClient(storageUri, credentials, options);
            }
            else
            {
                if (model.SASToken.StartsWith("BlobEndpoint"))
                {
                    // SAS Connection string
                    return new BlobServiceClient(model.SASToken.ToString(), options);
                }
                else if (model.SASToken.StartsWith("http"))
                {
                    // SAS Connection string
                    return new BlobServiceClient(new Uri(model.SASToken), options);
                }
                else if (model.SASToken.Contains("sig="))
                {
                    // SAS token
                    var url = new Uri($"{storageUri}?{model.SASToken}");
                    return new BlobServiceClient(url, options);
                }
                else
                {
                    // Account access key
                    var sharedKeyCredential = new StorageSharedKeyCredential(model.StorageAccountName, model.SASToken);
                    return new BlobServiceClient(storageUri, sharedKeyCredential, options);
                }
            }
        }
    }
}