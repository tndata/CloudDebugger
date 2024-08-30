using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CloudDebugger.Features.LogWorkspace;

/// <summary>
/// Send log statements to the Log Analytics workspace
/// 
/// Code inspiration https://github.com/microsoft/CILogProcessor/blob/main/CILogProcessor.cs
/// </summary>
public static class LogSender
{
    internal static async Task SendLogStatements(List<LogWorkspaceLogEntry> logStatements, string logType, string workspaceId, string workspaceKey)
    {
        string json = JsonConvert.SerializeObject(logStatements);

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

    private static string BuildSignature(string message, string datestring, string workspaceId, string workspaceKey)
    {
        // Create a hash for the API signature
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

        var _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("Log-Type", logName);
        _client.DefaultRequestHeaders.Add("Authorization", signature);
        _client.DefaultRequestHeaders.Add("x-ms-date", date);

        HttpContent httpContent = new StringContent(json, Encoding.UTF8);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await _client.PostAsync(new Uri(url), httpContent);
    }
}
