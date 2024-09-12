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
[EnableCors("MyCorsPolicy_wildcard")]
[Route("/api")]
[ApiController]
public class ApiEndpointsController : ControllerBase
{
    private readonly ILogger<ApiEndpointsController> _logger;
    private readonly static CustomerDatabase m_db = new();

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

        return Ok(DateTime.Now.ToString("HH:mm:ss"));
    }
}

