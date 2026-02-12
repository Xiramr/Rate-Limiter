using RateLimiter.Reader.Domain.Entities;
using System.Runtime.CompilerServices;

namespace RateLimiter.Reader.Domain.Interfaces;

public interface IRateLimitRepository
{
    IAsyncEnumerable<RateLimit> StreamAllAsync(
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<RateLimitChange> WatchAsync(
        CancellationToken cancellationToken = default);
}