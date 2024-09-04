using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.Redis;

public class RedisModel
{
    [Required]
    public string? Key { get; set; }
    public string? Value { get; set; }
    public int Expire { get; set; } = 30;

    /// <summary>
    /// Redis Connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }

    public List<string>? RedisKeys { get; set; }
}
