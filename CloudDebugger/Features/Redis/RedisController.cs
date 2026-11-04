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

    private const string connectionStringSessionKey = "RedisConnectionString";

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
            ConnectionString = HttpContext.Session.GetString(connectionStringSessionKey)
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

        if (ModelState.IsValid && model != null)
        {
            string connectionString = model.ConnectionString ?? "";

            // Remember the provided ConnectionString
            HttpContext.Session.SetString(connectionStringSessionKey, connectionString);

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
    /// Return all keys found in Redis that match the provided pattern.
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private static async Task<List<RedisKeyInfo>> ScanKeys(string pattern, string connectionString)
    {
        using ConnectionMultiplexer connection = await GetRedisConnection(connectionString);
        var server = connection.GetServer(connection.GetEndPoints()[0]);
        IDatabase db = connection.GetDatabase();

        var keys = new List<RedisKey>();
        long cursor = 0L;
        do
        {
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
        foreach (var key in keys.Distinct())
        {
            string keyString = key.ToString();
            if (keyString != null)
            {
                var type = await db.KeyTypeAsync(keyString);
                if (type == RedisType.None)
                    continue; // The key may have expired since SCAN returned it.

                string? value = type == RedisType.String
                    ? (string?)await db.StringGetAsync(keyString)
                    : "Value preview unavailable for this type.";
                TimeSpan? expiration = await db.KeyTimeToLiveAsync(keyString);

                resultingKeyList.Add(new RedisKeyInfo
                {
                    Key = keyString,
                    Type = type.ToString(),
                    Value = value,
                    Expiration = expiration
                });
            }
        }

        return resultingKeyList;
    }

    /// <summary>
    /// Write a key to Redis�
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expire"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private static async Task<string> WriteKeyToRedis(string key, string value, int expire, string connectionString)
    {
        using ConnectionMultiplexer connection = await GetRedisConnection(connectionString);
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

    /// <summary>
    /// Connect to redis using the access key or managed identity
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private static async Task<ConnectionMultiplexer> GetRedisConnection(string connectionString)
    {
        if (connectionString.Contains("password="))
        {
            // Connect using connection string
            return await ConnectionMultiplexer.ConnectAsync(connectionString);
        }
        else
        {
            // Connect using managed identity
            var configurationOptions = await ConfigurationOptions.Parse($"{connectionString}:6380").ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);

            return connectionMultiplexer;
        }
    }
}
