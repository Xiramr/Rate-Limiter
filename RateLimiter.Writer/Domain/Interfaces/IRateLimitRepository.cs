using RateLimiter.Writer.Domain.Entities;

namespace RateLimiter.Writer.Domain.Interfaces;

public interface IRateLimitRepository
{
    Task<bool> CreateLimitAsync(RateLimit limit, CancellationToken cancellationToken );
    Task<RateLimit?> GetLimitByRouteAsync(string route, CancellationToken cancellationToken );
    Task<bool> UpdateLimitAsync(RateLimit limit, CancellationToken cancellationToken );
    Task<bool> DeleteLimitAsync(string route, CancellationToken cancellationToken );
}