using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Api;

/// <summary>
/// REST API
/// 
/// This REST API endpoint provides a few useful API endpoints for testing purposes
/// 
/// /api/customers       Returns a JSON document with  100 customers
/// /api/customers/1     Returns a specific customer back as JSON (1-100)
/// /api/echo            Returns back the received request headers as a JSON document.
/// /api/time            Returns the current time as a string     
/// /api/cpu             Simulates CPU intensive tasks for 1 minute
/// /api/memory          Simulates memory intensive tasks for 1 minute
/// /api/customersSlow/1 Returns a specific customer back as JSON (1-100) with a delay
/// </summary>
[Route("/api")]
[ApiController]
[EnableCors("MyCorsPolicy_wildcard")]
public class ApiEndpointsController : ControllerBase
{
    private readonly ILogger<ApiEndpointsController> _logger;
    private static readonly CustomerDatabase m_db = new();

    public ApiEndpointsController(ILogger<ApiEndpointsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Return a list of customers back to the caller.
    /// </summary>
    /// <returns></returns>
    [HttpGet("customers")]
    public IEnumerable<Customer> GetAllCustomers()
    {
        _logger.LogInformation("/api/customers REST API endpoint was called");
        return m_db.GetAllCustomers();
    }

    /// <summary>
    /// Return a specific customer back to the caller.
    /// </summary>
    /// <param name="id">Customer Id</param>
    /// <returns></returns>
    [HttpGet("customers/{id}")]
    public ActionResult<Customer> GetSingleCustomer(int id)
    {
        _logger.LogInformation("/api/customers/{Id} REST API endpoint was called", id);

        var cust = m_db.GetCustomer(id);

        return cust != null
            ? Ok(cust)
            : NotFound();
    }

    /// <summary>
    /// Returns a specific customer (1-100), however, customer with ID=10 takes 1 second to execute. 
    /// All other takes a random time with a distribution centered around 50ms.
    /// </summary>
    /// <param name="id">Customer Id</param>
    /// <returns></returns>
    [HttpGet("CustomersSlow/{id}")]
    public async Task<ActionResult<Customer>> GetSingleCustomerSlow(int id)
    {
        _logger.LogInformation("/api/customers/{Id} REST API endpoint was called", id);

        var cust = m_db.GetCustomer(id);

        if (id == 10)
        {
            await Task.Delay(1500);
        }
        else
        {
            var random = new Random();
            double delay = Math.Abs(random.NextGaussian(50, 15)); // mean 50ms, stddev 15ms
            await Task.Delay((int)delay);
        }

        return cust != null
            ? Ok(cust)
            : NotFound();
    }

    /// <summary>
    /// Return a list of all the received request headers back to the caller.
    /// </summary>
    /// <returns></returns>
    [HttpGet("echo")]
    public ActionResult<RequestHeaders> Echo()
    {
        _logger.LogInformation("/api/echo REST API endpoint was called");

        var response = new RequestHeaders();
        foreach (var header in Request.Headers)
        {
            response.Headers.Add($"{header.Key}: {header.Value}");
        }

        return Ok(response);
    }

    /// <summary>
    /// Return the current time as a string back to the caller.
    /// </summary>
    /// <returns></returns>
    [HttpGet("time")]
    public ActionResult<string> CurrentTime()
    {
        _logger.LogInformation("/api/time REST API endpoint was called");

        return Ok(DateTime.UtcNow.ToString("HH:mm:ss"));
    }


    /// <summary>
    /// Simulates CPU intensive tasks for 1 minute
    /// </summary>
    [HttpGet("cpu")]
    public async Task<ActionResult<string>> Cpu()
    {
        _logger.LogInformation("/api/cpu REST API endpoint was called");

        using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
        {

            var tasks = new List<Task>();

            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var data = System.Text.Encoding.UTF8.GetBytes("Some CPU intensive task");
                        _ = System.Security.Cryptography.SHA256.HashData(data);
                    }
                }, cts.Token));
            }

            await Task.WhenAll(tasks);
        }

        return Ok("CPU intensive tasks completed");
    }



    /// <summary>
    /// Simulates memory intensive tasks for 1 minute
    /// </summary>
    /// <returns></returns>
    [HttpGet("memory")]
    public async Task<ActionResult<string>> Memory()
    {
        _logger.LogInformation("/api/memory REST API endpoint was called");

        using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var list = new List<byte[]>();
                    while (!cts.Token.IsCancellationRequested)
                    {
                        list.Add(new byte[1024 * 1024]); // Allocate 1MB
                        if (list.Count > 512) // Limit to 512 MB
                        {
                            list.Clear();
                        }
                    }
                }, cts.Token));
            }

            await Task.WhenAll(tasks);
        }

        return Ok("Memory intensive tasks completed");
    }
}
public static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
    {
        // Use the Box-Muller transform to generate a pair of independent standard normally distributed random numbers
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        return mean + stdDev * randStdNormal; // random normal(mean,stdDev^2)
    }
}

