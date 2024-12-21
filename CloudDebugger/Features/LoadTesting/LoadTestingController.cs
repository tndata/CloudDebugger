using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.LoadTesting;

/// <summary>
/// Controller for handling load testing operations.
/// </summary>
public class LoadTestingController : Controller
{
    public LoadTestingController()
    {
    }

    public IActionResult SendData()
    {
        var model = new LoadTestingModel()
        {
            TotalNumberOfRequests = 1000,
            NumberOfConcurrentRequests = 20,
            TargetURL = "https://[Enter Domain]/api/customersSlow/$ID"
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult SendData(LoadTestingModel model)
    {
        if (ModelState.IsValid && model != null)
        {
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(model.TargetURL);

                model.Exception = "";
                model.Message = "";

                string result = SendRequests(model.TargetURL, model.TotalNumberOfRequests, model.NumberOfConcurrentRequests);

                model.Message = result;
            }
            catch (Exception exc)
            {
                string msg = "";
                model.Exception = msg + exc.ToString();
            }
        }

        return View(model);
    }

    private const int RequestTimeoutSeconds = 5;
    private const int CancellationTimeoutSeconds = 180;

    private static string SendRequests(string targetURL, int totalNumberOfRequests, int numberOfConcurrentRequests)
    {
        int totalNumberOfRequestsSent = 0;
        int totalNumberOfSuccessFullRequests = 0;
        int totalNumberOfFailedRequests = 0;
        var random = new Random();

        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
            List<Task> tasks = [];
            using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(CancellationTimeoutSeconds)))
            {
                int requestsPerTask = totalNumberOfRequests / numberOfConcurrentRequests;

                for (int i = 0; i < numberOfConcurrentRequests; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int j = 0; j < requestsPerTask; j++)
                        {
                            try
                            {
                                string targetUrlWithId = targetURL.Replace("$ID", random.Next(1, 101).ToString()); //(1 - 100)
                                HttpResponseMessage response = await client.GetAsync(new Uri(targetUrlWithId), cts.Token);


                                Console.WriteLine($"Sent Request #{i} on Task {j} on TID {Environment.CurrentManagedThreadId} to {targetUrlWithId}");

                                if (response.IsSuccessStatusCode)
                                {
                                    Interlocked.Increment(ref totalNumberOfSuccessFullRequests);
                                }
                                else
                                {
                                    Interlocked.Increment(ref totalNumberOfFailedRequests);
                                }
                            }
                            catch (Exception exc)
                            {
                                Interlocked.Increment(ref totalNumberOfFailedRequests);
                            }
                            finally
                            {
                                Interlocked.Increment(ref totalNumberOfRequestsSent);
                            }
                        }
                    }, cts.Token));
                }

                Task.WaitAll(tasks.ToArray());
            }
        }

        return $"Total Requests Sent: {totalNumberOfRequestsSent}, Successful Requests: {totalNumberOfSuccessFullRequests}, Failed Requests: {totalNumberOfFailedRequests}";
    }
}
