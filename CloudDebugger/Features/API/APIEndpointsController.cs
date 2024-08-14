using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.API
{
    /// <summary>
    /// The various REST API Endpoints
    /// </summary>
    [EnableCors("MyCorsPolicy_wildcard")]
    [ApiController]
    public class APIEndpointsController : ControllerBase
    {
        private readonly ILogger<APIEndpointsController> _logger;
        private static CustomerDatabase m_db = new();

        public APIEndpointsController(ILogger<APIEndpointsController> logger)
        {
            _logger = logger;
        }

        [Route("/api/customers")]
        public IEnumerable<Customer> GetAllCustomers()
        {
            return m_db.GetAllCustomers();
        }

        [Route("/api/customers/{id}")]
        public ActionResult<Customer> GetSingleCustomer(int id)
        {
            var cust = m_db.GetCustomer(id);

            return cust != null
                ? Ok(cust)
                : NotFound();
        }


        [Route("/api/echo")]
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

        [Route("/api/time")]
        public ActionResult<RequestHeaders> CurrentTime()
        {
            return Ok(DateTime.Now.ToString("HH:mm:ss"));
        }
    }
}

