using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Api
{
    /// <summary>
    /// The various REST API Endpoints
    /// </summary>
    [EnableCors("MyCorsPolicy_wildcard")]
    [ApiController]
    public class ApiEndpointsController : ControllerBase
    {
        private readonly ILogger<ApiEndpointsController> _logger;
        private static readonly CustomerDatabase m_db = new();

        public ApiEndpointsController(ILogger<ApiEndpointsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/api/customers")]
        public IEnumerable<Customer> GetAllCustomers()
        {
            return m_db.GetAllCustomers();
        }

        [HttpGet("/api/customers/{id}")]
        public ActionResult<Customer> GetSingleCustomer(int id)
        {
            var cust = m_db.GetCustomer(id);

            return cust != null
                ? Ok(cust)
                : NotFound();
        }

        [HttpGet("/api/echo")]
        public ActionResult<RequestHeaders> Echo()
        {
            // Add the code to return all the request headers in the response

            var response = new RequestHeaders();
            foreach (var header in Request.Headers)
            {
                response.Headers.Add($"{header.Key}: {header.Value}");
            }

            return Ok(response);
        }

        [HttpGet("/api/time")]
        public ActionResult<RequestHeaders> CurrentTime()
        {
            return Ok(DateTime.Now.ToString("HH:mm:ss"));
        }
    }
}

