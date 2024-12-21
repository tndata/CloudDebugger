using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Api;

/// <summary>
/// REST API
/// 
/// This REST API endpoint provides a few useful API endpoints for testing purposes
/// 
/// /api/customers      Returns a JSON document with  100 customers
/// /api/customers/1    Returns a specific customer back as JSON (1-100)
/// /api/echo           Returns back the received request headers as a JSON document.
/// /api/time           Returns the current time as a string      
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
    /// Returns a specific customer (1-100) ,however, customer with ID=10 takes 1 second to execute. All other takes 20ms to complete.
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
            await Task.Delay(1000);
        }
        else
        {
            await Task.Delay(20);
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
                        var hash = System.Security.Cryptography.SHA256.HashData(data);
                    }
                }, cts.Token));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CPU intensive tasks were canceled after 1 minute");
            }
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

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Memory intensive tasks were canceled after 1 minute");
            }
        }

        return Ok("Memory intensive tasks completed");
    }
}

