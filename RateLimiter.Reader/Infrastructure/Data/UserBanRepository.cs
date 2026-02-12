using RateLimiter.Reader.Domain.Interfaces;
using StackExchange.Redis;

namespace RateLimiter.Reader.Infrastructure.Data;

public sealed class UserBanRepository : IUserBanRepository
{
    private static readonly LuaScript Script = LuaScript.Prepare(@"
local req_key = KEYS[1]
local ban_key = KEYS[2]
local limit = tonumber(ARGV[1])
local window_sec = tonumber(ARGV[2])
local ban_sec = tonumber(ARGV[3])

if redis.call('EXISTS', ban_key) == 1 then
    return 0
end

local current = redis.call('INCR', req_key)

local ttl = redis.call('TTL', req_key)
if ttl < 0 then
    redis.call('EXPIRE', req_key, window_sec)
end

if current > limit then
    redis.call('SET', ban_key, '1', 'EX', ban_sec, 'NX')
    return 0
end

return 1
");

    private readonly IDatabase _database;

    public UserBanRepository(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<bool> CheckAndProcessRequestAsync(
        int userId,
        string endpoint,
        int limit,
        TimeSpan window,
        TimeSpan banDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reqKey = (RedisKey)$"req:{userId}:{endpoint}";
        var banKey = (RedisKey)$"ban:{userId}:{endpoint}";

        var result = await _database.ScriptEvaluateAsync(
            Script,
            new RedisKey[] { reqKey, banKey },
            new RedisValue[]
            {
                limit,
                (long)window.TotalSeconds,
                (long)banDuration.TotalSeconds
            });

        return (int)result == 1;
    }
}