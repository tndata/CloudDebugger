using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace CloudDebugger.Features.Redis;

/// <summary>
/// 
/// </summary>
public class RedisController : Controller
{
    private readonly ILogger<RedisController> _logger;
    private static string connectionString = "";

    public RedisController(ILogger<RedisController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/Redis/ReadWriteKeys")]
    public IActionResult GetKeys(RedisModel model)
    {
        _logger.LogInformation("Redis.GetKeys called was called");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        model ??= new RedisModel();
        model.ConnectionString = connectionString;

        try
        {
            model.RedisKeys = ScanKeys("*");
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("ReadWriteKeys", model);
    }

    [HttpPost("/Redis/ReadWriteKeys")]
    public IActionResult WriteKey(RedisModel model)
    {
        _logger.LogInformation("Redis.WriteKey called was called");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        connectionString = model.ConnectionString ?? "";   //Remember connection string
        model.Exception = "";
        model.Message = "";

        if (model.Key == null || model.Value == null)
        {
            model.Message = "Key and Value are required.";
            return View("ReadWriteKeys", model);
        }

        try
        {
            model.Message = WriteKeyToRedis(model.Key, model.Value, model.Expire);
            model.RedisKeys = ScanKeys("*");
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }


        return View("ReadWriteKeys", model);
    }


    private static List<string> ScanKeys(string pattern)
    {
        var connection = ConnectionMultiplexer.Connect(connectionString);
        var server = connection.GetServer(connection.GetEndPoints()[0]);

        var keys = new List<RedisKey>();
        long cursor;
        do
        {
            cursor = 0L;
            var result = server.Execute("SCAN", cursor.ToString(), "MATCH", pattern, "COUNT", "100");
            if (result != null)
            {
                var innerResult = (RedisResult[])result!;
                if (innerResult != null && innerResult.Length > 1)
                {
                    cursor = (long)innerResult[0];
                    var keysResult = (RedisKey[])innerResult[1]!;
                    if (keysResult != null)
                    {
                        keys.AddRange(keysResult);
                    }
                }
            }
        } while (cursor != 0);

        var resultingKeyList = new List<string>();
        foreach (var key in keys)
        {
            resultingKeyList.Add(key.ToString());
        }

        return resultingKeyList;
    }

    private static string WriteKeyToRedis(string key, string value, int expire)
    {
        var connection = ConnectionMultiplexer.Connect(connectionString);
        IDatabase db = connection.GetDatabase();

        TimeSpan expiration = TimeSpan.FromSeconds(expire);

        // Set the key with expiration time
        bool isSet = db.StringSet(key, value, expiration);

        string result;
        if (isSet)
        {
            result = $"Key '{key}' set successfully with expiration time of {expiration.TotalMinutes} minutes.";
        }
        else
        {
            result = $"Failed to set key '{key}'.";
        }

        // Close the connection when done
        connection.Close();

        return result;
    }
}