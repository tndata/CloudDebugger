using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CloudDebugger.Features.RequestSender;

/// <summary>
/// HTTP Request sending tool
/// 
/// This tool allows you to send abritary HTTP(s) request from within your cloud debugger.
/// </summary>
public class RequestSenderController : Controller
{

    private static readonly string[] separator = ["\r\n", "\r", "\n"];


    public RequestSenderController()
    {
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new RequestSenderModel()
        {
            HttpMethod = "GET",
            URL = "https://httpbin.org/anything",
            RequestHeaders = """
                            Header1: Value1
                            User-Agent: Cloud Debugger
                            """
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(RequestSenderModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new RequestSenderModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            using (var request = BuildRequest(model))
            {
                var response = await SendRequestAsync(request);

                await ParseResponse(response, model);
            }
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }
        return View(model);
    }

    private static HttpRequestMessage BuildRequest(RequestSenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrEmpty(model.URL))
            throw new ArgumentException("model.URL cannot be null or empty", nameof(model));
        if (string.IsNullOrEmpty(model.HttpMethod))
            throw new ArgumentException("model.HttpMethod cannot be null or empty", nameof(model));

        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(model.HttpMethod),
            RequestUri = new Uri(model.URL.Trim())
        };

        if (!string.IsNullOrEmpty(model.RequestHeaders))
        {
            var headers = model.RequestHeaders.Split(separator, StringSplitOptions.None);
            foreach (var header in headers)
            {
                var headerParts = header.Split(':');
                if (headerParts.Length == 2)
                {
                    request.Headers.Add(headerParts[0].Trim(), headerParts[1].Trim());
                }
            }
        }

        if (!string.IsNullOrEmpty(model.AuthenticationToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.AuthenticationToken.Trim());
        }

        if (!string.IsNullOrEmpty(model.RequestBody))
        {
            request.Content = new StringContent(model.RequestBody, Encoding.UTF8);
        }

        return request;
    }
    private static async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        using (var client = new HttpClient())
        {
            return await client.SendAsync(request);
        }
    }
    private static async Task ParseResponse(HttpResponseMessage response, RequestSenderModel model)
    {
        model.ResponseStatusCode = $"{(int)response.StatusCode} {response.StatusCode}";
        model.ResponseHeaders = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

        if (response.Content != null)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            model.ResponseBody = responseBody;
        }
    }
}
