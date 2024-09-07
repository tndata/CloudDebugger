using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.Redis;

public class RedisModel
{
    [Required]
    public string? Key { get; set; }

    [Required]
    public string? Value { get; set; }

    [Required]
    public int ExpireSeconds { get; set; } = 30;

    /// <summary>
    /// Redis Connection string or Redis host name
    /// </summary>
    [Required]
    public string? ConnectionString { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// List of keys found in Redis
    /// </summary>
    public List<RedisKeyInfo>? RedisKeys { get; set; }
}
