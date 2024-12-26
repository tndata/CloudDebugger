using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CloudDebugger.Features.OpenTelemetry;

/// <summary>
/// Controller for handling OpenTelemetry operations.
/// 
/// The Azure Monitor Distro client SDK Documentation
/// https://github.com/Azure/azure-sdk-for-net/tree/Azure.Monitor.OpenTelemetry.AspNetCore_1.2.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore 
/// 
/// HttpClient and HttpWebRequest instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http
/// 
/// SqlClient Instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.SqlClient
/// </summary>
public class OpenTelemetryController : Controller
{
    // This ActivitySource must be added to the sources that OpenTelemetry listens to, otherwise activities from this source will be ignored.
    private static readonly ActivitySource activitySource = new("CloudDebugger");

    private static readonly Meter meter = new("CloudDebugger.Counters", "1.0");
    private static readonly Counter<int> simpleCounter = meter.CreateCounter<int>(name: "SimpleCounter", unit: "clicks", description: "A very simple counter");
    private static readonly Counter<int> advancedCounter = meter.CreateCounter<int>(name: "AdvancedCounter", unit: "clicks", description: "A slightly more advanceded counter");

    private static readonly string[] countries = { "Sweden", "Denmark", "Norway", "Finland" };
    private static readonly Random random = new();


    public IActionResult Index()
    {
        return View();
    }

    public IActionResult SimpleCounter()
    {
        var model = new SimpleCounterModel()
        {
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult SimpleCounter(SimpleCounterModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";

            var tags = new KeyValuePair<string, object?>[]
            {
                new("user.id", "user123"),
                new("operation.name", "UserClick")
            };

            simpleCounter.Add(1, tags);
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("SimpleCounter", model);
    }

    [HttpPost]
    public IActionResult AdvancedCounter(SimpleCounterModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";



            var tags = new KeyValuePair<string, object?>[]
            {
                new("country", GetRandomCountry()),
            };

            advancedCounter.Add(1, tags);
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("SimpleCounter", model);
    }


    public static string GetRandomCountry()
    {
        int index = random.Next(countries.Length);
        return countries[index];
    }



    /// <summary>
    /// Handles the submission of a custom event.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult CustomEvent(OpenTelemetryModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                // Start an activity
                using (var activity = activitySource.StartActivity("CustomEventOperation"))
                {
                    if (activity != null)
                    {
                        // Add custom event
                        activity.AddEvent(new ActivityEvent("MyCustomEvent", DateTime.UtcNow, new ActivityTagsCollection
                        {
                            { "event.id", Guid.NewGuid().ToString() },
                            { "event.description", "A custom event occurred" },
                            { "event.message", model.LogMessage },
                            { "event.service", "CloudDebugger" },
                            { "event.severity", "info" }
                        }));

                        // Custom attributes
                        activity.SetTag("custom.today", DateTime.UtcNow.Day);
                        activity.SetTag("custom.month", DateTime.UtcNow.Month);

                        model.Message = "Activity and custom event sent to OpenTelemetry.";
                    }
                    else
                    {
                        model.Message = "Tracing is not enabled";
                    }
                }
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }

    public IActionResult CreateTrace()
    {
        var model = new OpenTelemetryModel()
        {
            LogMessage = "My Custom Trace message"
        };

        return View(model);
    }

    /// <summary>
    /// Create a more complex trace
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateTrace(OpenTelemetryModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                model.Message = await GenerateTrace(model);
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }

    private static async Task<string> GenerateTrace(OpenTelemetryModel model)
    {
        using (var activity = activitySource.StartActivity(name: "Complex trace"))
        {
            if (activity == null)
            {
                // If activity is null, tracing is not enabled.
                return "Tracing is not enabled";
            }

            // Set tags for the activity (key-value metadata)
            activity.SetTag("operation.id", Guid.NewGuid().ToString());
            activity.SetTag("operation.name", "ComplexTraceOperation");
            activity.SetTag("user.id", "Joe-123");
            activity.SetTag("request.priority", "High");
            activity.SetTag("timestamp.start", DateTime.UtcNow.ToString("o"));

            // Add baggage items (key-value pairs that propagate downstream)
            Baggage.SetBaggage("bag.payment.id", Guid.NewGuid().ToString());
            Baggage.SetBaggage("bag.operation.context", "ComplexTrace");

            // Log an initial event with metadata
            activity.AddEvent(new ActivityEvent("Trace started", tags: new ActivityTagsCollection
            {
                { "event.severity", "info" },
                { "description", "Starting complex trace operation" }
            }));

            activity.AddEvent(new ActivityEvent("Processing workitem - step 1 (calling API)"));

            //This request will take two seconds to complete
            using (var client = new HttpClient())
            {
                await client.GetStringAsync(new Uri("https://httpbin.org/delay/2"));
            }

            activity.AddEvent(new ActivityEvent("Processing workitem - step 2"));
            await Task.Delay(250);

            activity.AddEvent(new ActivityEvent("Processing workitem - step 3"));
            await Task.Delay(500);

            var eventTags = new ActivityTagsCollection(list: new Dictionary<string, object?>
            {
                { "Step", "3" },
                { "Status", "OK" },
                { "message", model.LogMessage },
            });

            activity.AddEvent(new ActivityEvent("Processing the result - step 3", tags: eventTags));

            await Task.Delay(500);

            // Log completion of the operation
            activity.AddEvent(new ActivityEvent("Trace completed", tags: new ActivityTagsCollection
            {
                { "final.status", "Success" },
                { "completion.timestamp", DateTime.UtcNow.ToString("o") }
            }));

            return "Complex trace sent to OpenTelemetry";
        }
    }
}
