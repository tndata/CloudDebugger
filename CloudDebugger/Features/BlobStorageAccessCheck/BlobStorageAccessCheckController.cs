using Azure.Storage.Blobs;
using CloudDebugger.SharedCode.BlobStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobStorageAccessCheck;

public class BlobStorageAccessCheckController : Controller
{
    private const string authenticationApproach = "authenticationApproach";

    private const string storageAccountSessionKey = "blobStorageAccount";
    private const string containerSessionKey = "blobContainer";
    private const string sasTokenSessionKey = "blobSasToken";
    private const string blobToDeleteSessionKey = "blobToDeleteSasToken";
    private const string blobToReadAndUpdateSessionKey = "blobToReadAndUpdateSasToken";

    public IActionResult Index()
    {
        var model = new BlobStorageAccessCheckModel()
        {
            StorageAccountName = HttpContext.Session.GetString(storageAccountSessionKey) ?? "myfirststorageaccount42",
            ContainerName = HttpContext.Session.GetString(containerSessionKey) ?? "mycontainer",
            SASToken = HttpContext.Session.GetString(sasTokenSessionKey),

            // Default blob names for this tool. The tool expects these two blobs to exist in the container.
            BlobToDeleteName = HttpContext.Session.GetString(blobToDeleteSessionKey) ?? "BlobToDelete.json",
            BlobToReadUpdate = HttpContext.Session.GetString(blobToReadAndUpdateSessionKey) ?? "BlobToReadAndUpdate.json",
            AnonymousAccess = false
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Index(BlobStorageAccessCheckModel model)
    {
        if (!ModelState.IsValid || model == null)
            return BadRequest(ModelState);

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        string storageAccount = model.StorageAccountName?.Trim() ?? "";
        string containerName = model.ContainerName?.Trim() ?? "";
        string sasToken = model.SASToken?.Trim() ?? "";
        string blobToDeleteName = model.BlobToDeleteName?.Trim() ?? "";
        string blobToReadUpdate = model.BlobToReadUpdate?.Trim() ?? "";

        //Remember Storage Account and ContainerName
        HttpContext.Session.SetString(storageAccountSessionKey, storageAccount);
        HttpContext.Session.SetString(containerSessionKey, containerName);
        HttpContext.Session.SetString(sasTokenSessionKey, sasToken);
        HttpContext.Session.SetString(blobToDeleteSessionKey, sasToken);
        HttpContext.Session.SetString(blobToReadAndUpdateSessionKey, sasToken);

        try
        {
            (var client, var message) = BlobStorageClientBuilder.GetBlobServiceClient(model.StorageAccountName, model.SASToken, model.AnonymousAccess);
            ViewData[authenticationApproach] = message;

            var testsToExecute = new List<Func<BlobServiceClient, BlobStorageAccessCheckModel, BlogAccessTestResult>>();

            testsToExecute.Add(Service_TestReadPermission);



            ExecuteTests(testsToExecute, client, model);



        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        return View(model);
    }

    private void ExecuteTests(List<Func<BlobServiceClient, BlobStorageAccessCheckModel, BlogAccessTestResult>> testsToExecute, BlobServiceClient client, BlobStorageAccessCheckModel model)
    {

        foreach (Func<BlobServiceClient, BlobStorageAccessCheckModel, BlogAccessTestResult> test in testsToExecute)
        {
            try
            {
                var testResult = test(client, model);
                model.TestResults.Add(testResult);
            }
            catch (Exception exc)
            {
                var testrResult = new BlogAccessTestResult();
                testrResult.Passed = false;
                testrResult.Message = "Exception: " + exc.Message;
                model.TestResults.Add(testrResult);
            }
        }
    }

    private BlogAccessTestResult Service_TestReadPermission(BlobServiceClient? client, BlobStorageAccessCheckModel model)
    {
        return new BlogAccessTestResult();

    }
}
