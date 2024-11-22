using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CloudDebugger.SharedCode.QueueStorage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CloudDebugger.Features.QueueStorageReceiver;

/// <summary>
/// Azure Queue Storage receiver tool
/// 
/// This tool will send a number of messages to an Azure Queue Storage. 
/// It supports authentication using SAS Token or Managed Identity.
/// 
/// See the Wiki documentation on how to create a Storage Queue
/// 
/// Resources: 
/// * Documentation wiki
///   https://github.com/tndata/CloudDebugger/wiki/QueueStorage       
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
    private const int MaxNumberOfMessagesToRetrieve = 10;

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
            (QueueClient? client, string message) = QueueStorageClientBuilder.CreateQueueClient(new Uri(queueUrl), sasToken);
            ViewData[authenticationApproach] = message;

            using (var CTS = new CancellationTokenSource(WaitForMessagesTimeout))
            {
                QueueMessage[] messages = await client.ReceiveMessagesAsync(maxMessages: MaxNumberOfMessagesToRetrieve, cancellationToken: CTS.Token);

                model.ReceivedMessages = [];

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

                        //If the message is not deleted, the message will be invisible for default 30 seconds. (visibility timeout)
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
}
