namespace CloudDebugger.Features.Redis;

public class RedisKeyInfo
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public TimeSpan? Expiration { get; set; }
}