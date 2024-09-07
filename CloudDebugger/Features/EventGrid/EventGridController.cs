using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventGrid;

/// <summary>
/// Azure Event Grid client library for .NET
/// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?view=azure-dotnet
/// 
/// </summary>
public class EventGridController : Controller
{
    private readonly ILogger<EventGridController> _logger;

    private static string _accessKey = "";

    public EventGridController(ILogger<EventGridController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/EventGrid/SendEvents")]
    public IActionResult GetSendEvents(SendEventGridModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        model ??= new SendEventGridModel();
        model.AccessKey = _accessKey;

        return View("SendEvents", model);
    }

    [HttpPost("/EventGrid/SendEvents")]
    public IActionResult PostSendEvents(SendEventGridModel model)
    {
        if (model != null && ModelState.IsValid)
        {
            _accessKey = model.AccessKey ?? "";   //Remember access key
            model.Exception = "";
            model.Message = "";
            var userAssignedClientID = Environment.GetEnvironmentVariable("USERASSIGNEDCLIENTID") ?? "";

            try
            {
                EventGridPublisherClient? client = CreateEventGridClient(model, userAssignedClientID);

                int eventId = model.StartNumber;

                for (int i = 0; i < model.NumberOfEvents; i++)
                {
                    CloudEvent? @event = CreateEvent(eventId);

                    // Send the event
                    client.SendEventAsync(@event).Wait();

                    _logger.LogInformation("Sent event to Event Grid {Subject}:", @event.Subject);

                    eventId++;
                }

                model.StartNumber = eventId;
                model.Message = "Events sent!";

                if (!string.IsNullOrWhiteSpace(userAssignedClientID))
                {
                    model.Message = model.Message + " Using Entra ID identity " + userAssignedClientID;
                }
            }
            catch (Exception exc)
            {
                string msg = "";
                if (!string.IsNullOrWhiteSpace(userAssignedClientID))
                {
                    msg = "Using Entra ID identity " + userAssignedClientID + "\r\n";
                }

                model.Exception = msg + exc.ToString();
            }
        }

        return View("SendEvents", model);
    }

    private static EventGridPublisherClient CreateEventGridClient(SendEventGridModel model, string userAssignedClientID)
    {
        EventGridPublisherClient? client = null;
        if (string.IsNullOrWhiteSpace(_accessKey))
        {
            var defaultCredentialOptions = new DefaultAzureCredentialOptions();

            // See https://github.com/microsoft/azure-container-apps/issues/442
            // because one or more UserAssignedIdentity can be assigned to an Azure Resource, we have to be explicit about which one to use.

            if (!string.IsNullOrWhiteSpace(userAssignedClientID))
            {
                defaultCredentialOptions.ManagedIdentityClientId = userAssignedClientID;
            }

            client = new EventGridPublisherClient(new Uri(model.TopicEndpoint ?? ""),
                                                  new MyDefaultAzureCredential(defaultCredentialOptions));
        }
        else
        {
            client = new EventGridPublisherClient(new Uri(model.TopicEndpoint ?? ""),
                                                  new AzureKeyCredential(_accessKey));
        }

        return client;
    }

    private static CloudEvent CreateEvent(int eventId)
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
}


public class OrderEvent
{
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
}
public class ProductEvent
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
}
