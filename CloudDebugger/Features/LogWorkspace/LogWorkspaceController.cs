using CloudDebugger.Features.LogWorkspace;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CloudDebugger.Features.HomePage
{

    /// <summary>
    /// Send log data to Log Analytics with the HTTP Data Collector API
    /// https://learn.microsoft.com/en-us/rest/api/loganalytics/create-request
    /// </summary>
    public class LogWorkspaceController : Controller
    {
        private readonly ILogger<LogWorkspaceController> _logger;

        //"remember" the workspaceId and workspaceKey
        private static string workspaceId = "";
        private static string workspaceKey = "";

        // The name of the table to write to in Log Analytics workspace
        // We keep it hardcoded for now. The table will be automatically created if not found. 
        private const string logType = "MyApplicationLog";

        public LogWorkspaceController(ILogger<LogWorkspaceController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/LogWorkspace/SendEvents")]
        public IActionResult GetSendEvents(LogWorkspaceModel model)
        {
            if (model == null)
            {
                model = new LogWorkspaceModel();
            }

            model.WorkspaceId = workspaceId;
            model.WorkspaceKey = workspaceKey;
            model.logType = logType;
            ModelState.Clear();

            return View("SendEvents", model);
        }

        [HttpPost("/LogWorkspace/SendEvents")]
        public async Task<IActionResult> PostSendEvents(LogWorkspaceModel model)
        {
            model.Exception = "";
            model.Message = "";

            if (model != null && ModelState.IsValid && model.WorkspaceId != null && model.WorkspaceKey != null)
            {
                try
                {
                    var logStatements = GenerateLogStatements(model.LogMessage);

                    await SendLogStatements(logStatements, logType, model.WorkspaceId, model.WorkspaceKey);

                    model.Message = $"Log statements sent to table {logType} in your Log Analytics Workspace! It will take a few minutes before the data shows up in the table.";
                }
                catch (Exception exc)
                {
                    string msg = "";
                    model.Exception = msg + exc.ToString();
                }
            }

            return View("SendEvents", model);
        }

        private Task SendLogStatements(List<LogWorkspaceLogEntry> logStatements, string logType, string workspaceId, string workspaceKey)
        {
            var sender = new LogSender();

            return sender.SendLogStatements(logStatements, logType, workspaceId, workspaceKey);
        }


        private List<LogWorkspaceLogEntry> GenerateLogStatements(string? logMessage)
        {
            var tmp = new List<LogWorkspaceLogEntry>();

            var severity = new List<string>() { "Information", "Warning", "Error" };

            for (int i = 0; i < 10; i++)
            {
                //Select a random severity  
                var random = new Random();
                var index = random.Next(severity.Count);
                var logData = new LogWorkspaceLogEntry()

                {
                    Message = logMessage + " #" + i,
                    Severity = severity[index],
                    Timestamp = DateTime.UtcNow
                };

                tmp.Add(logData);
            }

            return tmp;

        }

        //public async Task<IActionResult> Workspace()
        //{


        //    var logData = new LogWorkspaceLogEntry()
        //    {
        //        Message = "This is a test log message",
        //        Severity = "Information",
        //        Timestamp = DateTime.UtcNow
        //    };

        //    CILogProcessor.SendData(logData);




        //    //var logAnalyticsHelper = new LogAnalyticsHelper();

        //    //await logAnalyticsHelper.SendLogAsync(WorkspaceId, WorkspaceKey, logType, logData);


        //    return View();
        //}

    }


    /// <summary>
    /// Send log statements to the Log Analytics workspace
    /// 
    /// Code inspiration https://github.com/microsoft/CILogProcessor/blob/main/CILogProcessor.cs
    /// </summary>
    public class LogSender
    {
        internal async Task SendLogStatements(List<LogWorkspaceLogEntry> logStatements, string logType, string workspaceId, string workspaceKey)
        {
            var _client = new HttpClient();

            string json = JsonConvert.SerializeObject(logStatements);
            //var jsonMessages = $"[{String.Join(",", json)}]";

            //Generate signature as specified in Log Analytics Data collector API 
            //https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api#sample-requests
            var datestring = DateTime.UtcNow.ToString("r");
            var signature = BuildSignature(json, datestring, workspaceId, workspaceKey);

            var response = await PostData(signature, datestring, json, logType, workspaceId);
            var responseContent = response.Content;
            string result = await responseContent.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send request to Log Analytics Data Collector, response: {result}");
            }
        }

        public string BuildSignature(string message, string datestring, string workspaceId, string workspaceKey)
        {
            // Create a hash for the API signature
            var jsonBytes = Encoding.UTF8.GetBytes(message);
            string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";

            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(workspaceKey);
            byte[] messageBytes = encoding.GetBytes(stringToHash);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return "SharedKey " + workspaceId + ":" + Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        public static async Task<HttpResponseMessage> PostData(string signature, string date, string json, string logName, string workspaceId)
        {
            string url = "https://" + workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

            System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("Log-Type", logName);
            _client.DefaultRequestHeaders.Add("Authorization", signature);
            _client.DefaultRequestHeaders.Add("x-ms-date", date);
            //_client.DefaultRequestHeaders.Add("time-generated-field", );

            System.Net.Http.HttpContent httpContent = new StringContent(json, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return await _client.PostAsync(new Uri(url), httpContent);
        }
    }
}
