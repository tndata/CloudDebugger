using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CloudDebugger.SharedCode.LogAnalyticsWorkspace;

/// <summary>
/// Send log statements to the Log Analytics workspace
/// 
/// The table is automatically created when we send data to it.
/// 
/// Code inspiration https://github.com/microsoft/CILogProcessor/blob/main/CILogProcessor.cs
/// </summary>
public static class LogAnalyticsWorkspaceSender
{
    /// <summary>
    /// Sends log statements to the Log Analytics workspace.
    /// The table is automatically created when data is sent to it.
    /// 
    /// Code inspiration: https://github.com/microsoft/CILogProcessor/blob/main/CILogProcessor.cs
    /// </summary>
    /// <param name="LogEntries">List of log entries to be sent.</param>
    /// <param name="logType">Type of the log.</param>
    /// <param name="workspaceId">Workspace ID for Log Analytics.</param>
    /// <param name="workspaceKey">Workspace key for Log Analytics.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request to Log Analytics Data Collector fails.</exception>
    /// </summary>
    /// <param name="LogEntries"></param>
    /// <param name="logType"></param>
    /// <param name="workspaceId"></param>
    /// <param name="workspaceKey"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static async Task SendLogStatements(List<LogWorkspaceLogEntry> LogEntries, string logType, string workspaceId, string workspaceKey)
    {
        foreach (var entry in LogEntries)
        {
            string json = JsonConvert.SerializeObject(entry);

            //Generate signature as specified in Log Analytics Data collector API 
            //https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api#sample-requests
            var datestring = DateTime.UtcNow.ToString("r");

            var signature = BuildSignature(json, datestring, workspaceId, workspaceKey);

            var response = await PostData(signature, datestring, json, logType, workspaceId);
            var responseContent = response.Content;
            string result = await responseContent.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to send request to Log Analytics Data Collector, response: {result}");
            }
        }
    }

    /// <summary>
    /// Builds the HMAC-SHA256 signature required by the Log Analytics Data Collector API.
    /// Azure requires this specific signing format for authentication.
    /// See: https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-collector-api
    /// </summary>
    private static string BuildSignature(string message, string datestring, string workspaceId, string workspaceKey)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(message);
        string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";

        var encoding = new ASCIIEncoding();
        byte[] keyByte = Convert.FromBase64String(workspaceKey);
        byte[] messageBytes = encoding.GetBytes(stringToHash);
        using (var hmacsha256 = new HMACSHA256(keyByte))
        {
            byte[] hash = hmacsha256.ComputeHash(messageBytes);
            return "SharedKey " + workspaceId + ":" + Convert.ToBase64String(hash);
        }
    }

    // Send a request to the POST API endpoint
    private static async Task<HttpResponseMessage> PostData(string signature, string date, string json, string logName, string workspaceId)
    {
        string url = "https://" + workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

        using (var _client = new HttpClient())
        {
            using (var httpContent = new StringContent(json, Encoding.UTF8))
            {
                _client.DefaultRequestHeaders.Add("Accept", "application/json");
                _client.DefaultRequestHeaders.Add("Log-Type", logName);
                _client.DefaultRequestHeaders.Add("Authorization", signature);
                _client.DefaultRequestHeaders.Add("x-ms-date", date);

                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return await _client.PostAsync(new Uri(url), httpContent);
            }
        }
    }
}
