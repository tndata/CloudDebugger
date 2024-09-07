using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace CloudDebugger.Features.Redis;

/// <summary>
/// RedisController
/// ===============
/// 
/// Resources:
/// https://learn.microsoft.com/en-gb/azure/azure-cache-for-redis/cache-managed-identity
/// https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/cache-azure-active-directory-for-authentication
/// https://github.com/Azure/Microsoft.Azure.StackExchangeRedis
/// </summary>
public class RedisController : Controller
{
    private readonly ILogger<RedisController> _logger;
    private const string sessionKey = "RedisConnectionString";

    public RedisController(ILogger<RedisController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> ReadWriteKeys()
    {
        _logger.LogInformation("Redis.ReadWriteKeys (GET) called was called");

        var model = new RedisModel()
        {
            ConnectionString = HttpContext.Session.GetString(sessionKey)
        };

        if (!string.IsNullOrEmpty(model.ConnectionString))
        {
            try
            {
                // Try to list the existing keys in Redis
                model.RedisKeys = await ScanKeys("*", model.ConnectionString);
            }
            catch (Exception exc)
            {
                model.Exception = exc.ToString();
            }
        }

        return View(model);
    }

    [HttpPost()]
    public async Task<IActionResult> ReadWriteKeys(RedisModel model)
    {
        _logger.LogInformation("Redis.ReadWriteKeys (POST) called was called");

        if (ModelState.IsValid)
        {
            string connectionString = model.ConnectionString ?? "";

            // Remember the ConnectionString
            HttpContext.Session.SetString(sessionKey, connectionString);

            try
            {
                model.Message = await WriteKeyToRedis(model.Key ?? "", model.Value ?? "", model.ExpireSeconds, connectionString);

                // Try to list the existing keys in Redis
                model.RedisKeys = await ScanKeys("*", connectionString);
            }
            catch (Exception exc)
            {
                model.Exception = exc.ToString();
            }
        }

        return View(model);
    }


    /// <summary>
    /// Return a list of the 100 first keys found in redis that matches the provided pattern. 
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private async static Task<List<RedisKeyInfo>> ScanKeys(string pattern, string connectionString)
    {
        ConnectionMultiplexer connection = await GetRedisConnection(connectionString);
        var server = connection.GetServer(connection.GetEndPoints()[0]);
        IDatabase db = connection.GetDatabase();

        var keys = new List<RedisKey>();
        long cursor;
        do
        {
            cursor = 0L;
            var result = await server.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", pattern, "COUNT", "100");
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


        // Convert the result to a list of RedisKeyInfo
        var resultingKeyList = new List<RedisKeyInfo>();
        foreach (var key in keys)
        {
            string keyString = key.ToString();
            if (keyString != null)
            {
                var value = await db.StringGetAsync(keyString);
                TimeSpan? expiration = await db.KeyTimeToLiveAsync(keyString);

                resultingKeyList.Add(new RedisKeyInfo
                {
                    Key = keyString,
                    Value = value,
                    Expiration = expiration
                });
            }
        }

        return resultingKeyList;
    }

    /// <summary>
    /// Write a key to Redis
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expire"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private async static Task<string> WriteKeyToRedis(string key, string value, int expire, string connectionString)
    {
        ConnectionMultiplexer connection = await GetRedisConnection(connectionString);
        IDatabase db = connection.GetDatabase();

        TimeSpan expiration = TimeSpan.FromSeconds(expire);

        // Set the key with expiration time
        bool isSet = await db.StringSetAsync(key, value, expiration);

        string result;
        if (isSet)
        {
            result = $"Key '{key}' set successfully with expiration time of {expiration.TotalSeconds} seconds.";
        }
        else
        {
            result = $"Failed to set key '{key}'.";
        }

        // Close the connection when done
        await connection.CloseAsync();

        return result;
    }

    private async static Task<ConnectionMultiplexer> GetRedisConnection(string connectionString)
    {
        if (connectionString.Contains("password="))
        {
            // Connect using connection string
            return await ConnectionMultiplexer.ConnectAsync(connectionString);
        }
        else
        {
            // Connect using managed identity
            var configurationOptions = await ConfigurationOptions.Parse($"{connectionString}:6380").ConfigureForAzureWithTokenCredentialAsync(new MyDefaultAzureCredential());

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);

            return connectionMultiplexer;
        }
    }
}
