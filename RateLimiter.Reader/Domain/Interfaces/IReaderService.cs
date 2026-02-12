using RateLimiter.Reader.Domain.Entities;

namespace RateLimiter.Reader.Domain.Interfaces;

public interface IReaderService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task WatchAsync(CancellationToken cancellationToken = default);
    RateLimit[] GetAllLimits();
    bool TryGetLimit(string route, out RateLimit? limit);
}