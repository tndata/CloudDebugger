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
    /// Redis Connection string or host name
    /// </summary>
    [Required]
    public string? ConnectionString { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }

    public List<RedisKeyInfo>? RedisKeys { get; set; }
}
