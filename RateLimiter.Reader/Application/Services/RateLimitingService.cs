using Microsoft.Extensions.Options;
using RateLimiter.Reader.Domain.Configuration;
using RateLimiter.Reader.Domain.Interfaces;
using RateLimiter.Reader.Transport;

namespace RateLimiter.Reader.Application.Services;

public sealed class RateLimitingService : IRateLimitingService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly IReaderService _readerService;
    private readonly IUserBanRepository _banRepository;
    private readonly RedisOptions _redisOptions;

    public RateLimitingService(
        IReaderService readerService,
        IUserBanRepository banRepository,
        IOptions<RedisOptions> redisOptions)
    {
        _readerService = readerService;
        _banRepository = banRepository;
        _redisOptions = redisOptions.Value;
    }

    public async Task ProcessRequestAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        if (!_readerService.TryGetLimit(request.Endpoint, out var limitConfig) || limitConfig is null)
            return;

        var banDuration = TimeSpan.FromMinutes(_redisOptions.BanDurationMinutes);

        await _banRepository.CheckAndProcessRequestAsync(
            request.UserId,
            request.Endpoint,
            limitConfig.RequestsPerMinute,
            Window,
            banDuration,
            cancellationToken);
    }
}