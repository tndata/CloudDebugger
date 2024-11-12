using Azure;
using Azure.MyIdentity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CloudDebugger.Features.QueueStorageReceiver;

/// <summary>
/// Azure Queue Storage receiver tool
/// 
/// This tool will send a number of messages to an Azure Queue Storage. It supposed authentication using SAS Token or Managed Identity.
/// 
/// Resources: 
/// * Getting Started with Azure Queue Service in .NET
///   https://github.com/Azure-Samples/storage-queue-dotnet-getting-started
/// * Azure Storage client libraries for .NET
///   https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage
/// </summary>
public class QueueStorageReceiverController : Controller
{

    // Keys for the Session Storage
    private const string QueueUrlSessionKey = "QueueStorageUrlString";
    private const string queueSasTokenSessionKey = "QueueStorageSasTokenString";

    private const string authenticationApproach = "authenticationApproach";
    private const int WaitForMessagesTimeout = 10000;       //ms

    public QueueStorageReceiverController()
    {
    }

    [HttpGet]
    public IActionResult ReceiveMessages()
    {
        var model = new ReceiveMessagesModel()
        {
            SasToken = HttpContext.Session.GetString(queueSasTokenSessionKey) ?? "",
            QueueUrl = HttpContext.Session.GetString(QueueUrlSessionKey) ?? "",
        };

        return View(model);
    }

    /// <summary>
    /// Receive events from the Queue Storage.
    /// 
    /// It will:
    /// * receive up to 10 messages
    /// * It will wait for up to 10 seconds, then give up
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> ReceiveMessages(ReceiveMessagesModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new ReceiveMessagesModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        ViewData[authenticationApproach] = "";

        string sasToken = model.SasToken ?? "";
        string queueUrl = model.QueueUrl ?? "";

        //Remember these values in the Session
        HttpContext.Session.SetString(queueSasTokenSessionKey, sasToken);
        HttpContext.Session.SetString(QueueUrlSessionKey, queueUrl);

        try
        {
            QueueClient? client = CreateQueueClient(queueUrl, sasToken);

            using (var CTS = new CancellationTokenSource(WaitForMessagesTimeout))
            {
                QueueMessage[] messages = await client.ReceiveMessagesAsync(maxMessages: 10, cancellationToken: CTS.Token);

                model.ReceivedMessages = new List<string>();

                foreach (QueueMessage receivedMessage in messages)
                {
                    var messageIdentifier = receivedMessage.MessageId;

                    var popReceipt = receivedMessage.PopReceipt;
                    var body = receivedMessage.Body.ToString();
                    var dequeueCount = receivedMessage.DequeueCount;

                    var nextVisibleOn = receivedMessage.NextVisibleOn?.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz") ?? "Null";
                    var insertedOn = receivedMessage.InsertedOn?.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz") ?? "Null";
                    var expiresOn = receivedMessage.ExpiresOn?.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz") ?? "Null";

                    var sb = new StringBuilder();
                    sb.AppendLine($"messageIdentifier: {messageIdentifier}");
                    sb.AppendLine($"insertedOn: {insertedOn} expiresOn: {expiresOn} nextVisibleOn: {nextVisibleOn}");
                    sb.AppendLine($"popReceipt: {popReceipt}");
                    sb.AppendLine($"dequeueCount: {dequeueCount}");
                    sb.AppendLine($"body: {body}");

                    model.ReceivedMessages.Add(sb.ToString());

                    if (model.DeleteMessagesAfterRead)
                    {
                        // Let the service know we're finished with
                        // the message and it can be safely deleted.
                        await client.DeleteMessageAsync(receivedMessage.MessageId, receivedMessage.PopReceipt);
                    }
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
    /// Create an instance of the Queue Storage client
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="queueUrl"></param>
    /// <param name="queueName"></param>
    /// <returns></returns>
    private QueueClient CreateQueueClient(string queueUrl, string sasToken)
    {
        if (string.IsNullOrWhiteSpace(sasToken))
        {
            // See https://github.com/microsoft/azure-container-apps/issues/442
            // because one or more UserAssignedIdentity can be assigned to an Azure Resource, we have to be explicit about which one to use.

            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";

            var defaultCredentialOptions = new DefaultAzureCredentialOptions();

            if (string.IsNullOrEmpty(clientId))
            {
                ViewData[authenticationApproach] = "Tried to authenticate using system-assigned managed identity";
            }
            else
            {
                ViewData[authenticationApproach] = $"Tried to authenticate using-assigned managed identity, ClientId={clientId}";
                defaultCredentialOptions.ManagedIdentityClientId = clientId;
            }

            return new QueueClient(new Uri(queueUrl), new MyDefaultAzureCredential(defaultCredentialOptions));
        }
        else
        {
            //Authenticate using SAS Token
            ViewData[authenticationApproach] = "Tried to authenticate using Queue Service SAS URL";

            var credentials = new AzureSasCredential(sasToken);
            return new QueueClient(new Uri(queueUrl), credentials);
        }
    }
}
