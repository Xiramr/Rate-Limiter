using Grpc.Core;
using Grpc.Core.Interceptors;
using UserService.Domain.Interfaces;

namespace UserService.Grpc.Interceptors;

public sealed class RateLimitInterceptor : Interceptor
{
    private readonly IUserBanService _userBanService;

    public RateLimitInterceptor(IUserBanService userBanService)
    {
        _userBanService = userBanService;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (!context.UserState.TryGetValue("user_id", out var userIdObj) || userIdObj is not int userId)
        {
            return await continuation(request, context);
        }

        var methodName = GetMethodName(context.Method);

        var isBanned = await _userBanService.IsUserBannedAsync(userId, methodName, context.CancellationToken);

        if (isBanned)
        {
            throw new RpcException(new Status(
                StatusCode.ResourceExhausted,
                $"Rate limit exceeded for endpoint '{methodName}'. Please try again later."));
        }

        return await continuation(request, context);
    }

    private static string GetMethodName(string fullMethodName)
    {
        var parts = fullMethodName.Split('/');
        return parts.Length > 0 ? parts[^1] : fullMethodName;
    }
}