namespace RateLimiter.Reader.Domain.Interfaces;

public interface IUserBanRepository
{
    Task<bool> CheckAndProcessRequestAsync(
        int userId,
        string endpoint,
        int limit,
        TimeSpan window,
        TimeSpan banDuration,
        CancellationToken cancellationToken = default);
}