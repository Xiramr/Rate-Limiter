using StackExchange.Redis;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Repositories;

public class UserBanRepository : IUserBanRepository
{
    private readonly IDatabase _redis;

    public UserBanRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public Task<bool> IsUserBannedAsync(int userId, string endpoint, CancellationToken cancellationToken = default)
    {
        var banKey = $"ban:{userId}:{endpoint}";
        return _redis.KeyExistsAsync(banKey);
    }
}