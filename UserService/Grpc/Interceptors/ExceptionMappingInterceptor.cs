using Grpc.Core;
using Grpc.Core.Interceptors;
using UserService.Domain.Exceptions;

namespace UserService.Grpc.Interceptors;

public sealed class ExceptionMappingInterceptor : Interceptor
{
    private readonly ILogger<ExceptionMappingInterceptor> _logger;

    public ExceptionMappingInterceptor(ILogger<ExceptionMappingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (UserAlreadyExistsException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}