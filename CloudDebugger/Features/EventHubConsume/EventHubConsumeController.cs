using Azure.Messaging.EventHubs.Consumer;
using CloudDebugger.Features.EventHubProduce;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventHubConsume;

/// <summary>
/// Azure Event Hub client library for .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/sdks
/// 
/// Quickstart: Send events to and receive events from Azure Event Hubs using .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send
/// </summary>
public class EventHubConsumeController : Controller
{
    private readonly ILogger<EventHubConsumeController> _logger;
    private const string connectionSessionKey = "EventHubConnectionString";
    private const string consumerGroupSessionKey = "EventHubConsumerGroup";

    public EventHubConsumeController(ILogger<EventHubConsumeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult ConsumeEvents()
    {
        var model = new ConsumeEventHubModel()
        {
            ConnectionString = HttpContext.Session.GetString(connectionSessionKey),
            ConsumerGroup = HttpContext.Session.GetString(consumerGroupSessionKey) ?? "$Default"
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConsumeEvents(ConsumeEventHubModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new ConsumeEventHubModel());

        var ConnectionString = model.ConnectionString ?? "";
        var consumerGroup = model.ConsumerGroup ?? "$Default";

        //Remember ConnectionString
        HttpContext.Session.SetString(connectionSessionKey, ConnectionString);
        HttpContext.Session.SetString(consumerGroupSessionKey, consumerGroup);

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            EventHubConsumerClient consumerClient = GetConsumerClient(ConnectionString, consumerGroup);

            var options = new ReadEventOptions()
            {
                MaximumWaitTime = TimeSpan.FromSeconds(1)  //If specified, should there be no events available before this waiting period expires, an empty event will be returned, allowing control to return to the reader that was waiting.
            };

            // It is important to note that this method does not guarantee fairness amongst the partitions during iteration.
            // each of the partitions compete to publish events to be read by the enumerator. Depending on service communication
            // there may be a clustering of events per partition and/or there may be a noticeable bias for a given partition or
            // subset of partitions.
            // https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient.readeventsasync
            await foreach (PartitionEvent @event in consumerClient.ReadEventsAsync(startReadingAtEarliestEvent: true,
                                                                        readOptions: options))
            {
                // Exit when we receive an emtpty event
                if (@event.Data == null)
                    break;

                var entry = ParseEvent(@event);
                model.Events.Add(entry);
            }
        }
        catch (Exception exc)
        {

            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;

            _logger.LogError(exc, "Failure when consuming events from EventHub");

        }

        return View(model);
    }


    private static EventHubLogEntry ParseEvent(PartitionEvent @event)
    {
        var entry = new EventHubLogEntry()
        {
            EventDetails = []
        };

        var contentType = @event.Data.ContentType;
        if (!String.IsNullOrWhiteSpace(contentType))
            entry.EventDetails.Add($"ContentType: {contentType}");

        string correlationId = @event.Data.CorrelationId;
        if (!String.IsNullOrWhiteSpace(correlationId))
            entry.EventDetails.Add($"CorrelationId: {correlationId}");

        DateTimeOffset enqueuedTime = @event.Data.EnqueuedTime;
        entry.EventDetails.Add($"EnqueuedTime: {enqueuedTime:o}"); // ISO 8601 format

        string messageId = @event.Data.MessageId;
        if (!String.IsNullOrWhiteSpace(messageId))
            entry.EventDetails.Add($"MessageId: {messageId}");

        string partitionKey = @event.Data.PartitionKey;
        if (!String.IsNullOrWhiteSpace(partitionKey))
            entry.EventDetails.Add($"PartitionKey: {partitionKey}");

        long sequenceNumber = @event.Data.SequenceNumber;
        entry.EventDetails.Add($"SequenceNumber: {sequenceNumber}");

        entry.Offset = @event.Data.Offset;

        var properties = @event.Data.Properties;
        foreach (var property in properties)
        {
            entry.EventDetails.Add($"{property.Key}: {property.Value}");
        }

        var systemProperties = @event.Data.SystemProperties;
        foreach (var property in systemProperties)
        {
            entry.EventDetails.Add($"{property.Key}: {property.Value}");
        }

        entry.PartitionId = @event.Partition.PartitionId;

        entry.Body = @event.Data.EventBody.ToString();

        return entry;
    }

    private static EventHubConsumerClient GetConsumerClient(string connectionString, string consumerGroup)
    {
        var consumerClient = new EventHubConsumerClient(consumerGroup: consumerGroup, connectionString: connectionString);

        return consumerClient;
    }
}
