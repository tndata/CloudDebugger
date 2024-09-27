using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.MyIdentity;
using CloudDebugger.Features.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventGrid;

/// <summary>
/// Event Grid
/// ==========
/// A simple tool to send custom events to Azure Event Grid
/// 
/// It will alternate between sending a OrderEvent and ProductEvent
/// 
/// Azure Event Grid client library for .NET
/// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?view=azure-dotnet
/// 
/// </summary>
public class EventGridController : Controller
{
    private readonly ILogger<EventGridController> _logger;
    private const string accessSessionKey = "EventGridAccessKey";
    private const string topicSessionKey = "EventGridTopic";
    private const string authenticationApproach = "authenticationApproach";

    public EventGridController(ILogger<EventGridController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SendEvents()
    {
        var model = new SendEventGridModel()
        {
            AccessKey = HttpContext.Session.GetString(accessSessionKey),
            TopicEndpoint = HttpContext.Session.GetString(topicSessionKey)
        };

        return View(model);
    }


    [HttpPost]
    public IActionResult SendEvents(SendEventGridModel model, string button)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new ReadWriteFilesModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();
        ViewData[authenticationApproach] = "";

        string accessKey = model.AccessKey ?? "";
        string topicEndpoint = model.TopicEndpoint ?? "";

        //Remember access key and topic
        HttpContext.Session.SetString(accessSessionKey, accessKey);
        HttpContext.Session.SetString(topicSessionKey, topicEndpoint);

        try
        {
            EventGridPublisherClient? client = CreateEventGridClient(accessKey, topicEndpoint);

            switch (button)
            {
                case "eventgrid":
                    SendEventGridEvents(model, client);
                    break;
                case "cloudevent":
                    SendCloudEvents(model, client);
                    break;
            }
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        return View(model);
    }

    private void SendCloudEvents(SendEventGridModel model, EventGridPublisherClient client)
    {
        int eventId = model.StartNumber;

        for (int i = 0; i < model.NumberOfEvents; i++)
        {
            CloudEvent? @event = CreateCloudEvent(eventId);

            // Send the event
            client.SendEventAsync(@event).Wait();

            _logger.LogInformation("Sent event to Event Grid {Subject} using the CloudEvent schema", @event.Subject);

            eventId++;
        }

        model.StartNumber = eventId;
        model.Message = $"{model.NumberOfEvents} events sent using the CloudEvent schema!";
    }

    private void SendEventGridEvents(SendEventGridModel model, EventGridPublisherClient client)
    {
        int eventId = model.StartNumber;

        for (int i = 0; i < model.NumberOfEvents; i++)
        {
            EventGridEvent? @event = CreateEventGridEvent(eventId);

            // Send the event
            client.SendEventAsync(@event).Wait();

            _logger.LogInformation("Sent event to Event Grid {Subject} using the EventGrid schema", @event.Subject);

            eventId++;
        }

        model.StartNumber = eventId;
        model.Message = $"{model.NumberOfEvents} events sent using the EventGrid schema!";
    }


    private static CloudEvent CreateCloudEvent(int eventId)
    {
        if (eventId % 2 == 0)
        {
            var order = new OrderEvent()
            {
                OrderId = eventId,
                CustomerName = "Customer " + eventId
            };

            var @event = new CloudEvent(source: "CloudDebugger", type: "BusinessEvent.NewOrder", jsonSerializableData: order)
            {
                Subject = "Order" + order.OrderId
            };

            Console.WriteLine(@event.Subject);
            return @event;
        }
        else
        {
            var product = new ProductEvent()
            {
                ProductId = eventId,
                ProductName = "Product " + eventId
            };

            var @event = new CloudEvent(source: "CloudDebugger", type: "BusinessEvent.NewOrder", jsonSerializableData: product)
            {
                Subject = "Product" + product.ProductId
            };
            return @event;
        }
    }


    private static EventGridEvent CreateEventGridEvent(int eventId)
    {
        if (eventId % 2 == 0)
        {
            var order = new OrderEvent()
            {
                OrderId = eventId,
                CustomerName = "Customer " + eventId
            };

            var @event = new EventGridEvent(
                subject: "Order" + order.OrderId,
                eventType: "BusinessEvent.NewOrder",
                dataVersion: "1.0",
                data: order)
            {
                EventTime = DateTime.UtcNow
            };

            Console.WriteLine(@event.Subject);
            return @event;
        }
        else
        {
            var product = new ProductEvent()
            {
                ProductId = eventId,
                ProductName = "Product " + eventId
            };

            var @event = new EventGridEvent(
                subject: "Product" + product.ProductId,
                eventType: "BusinessEvent.NewProduct",
                dataVersion: "1.0",
                data: product)
            {
                EventTime = DateTime.UtcNow
            };

            return @event;
        }
    }


    private EventGridPublisherClient CreateEventGridClient(string accessKey, string topicEndpoint)
    {

        if (string.IsNullOrWhiteSpace(accessKey))
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

            return new EventGridPublisherClient(new Uri(topicEndpoint),
                                                  new MyDefaultAzureCredential(defaultCredentialOptions));
        }
        else
        {
            ViewData[authenticationApproach] = "Tried to authenticate using access key";
            return new EventGridPublisherClient(new Uri(topicEndpoint),
                                                new AzureKeyCredential(accessKey));
        }
    }
}
