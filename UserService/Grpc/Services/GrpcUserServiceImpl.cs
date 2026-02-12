using FluentValidation;
using Grpc.Core;
using UserService.Application.Mappers;
using UserService.Domain.Interfaces;
using UserService.Protos;

namespace UserService.Grpc.Services;

public class GrpcUserServiceImpl : UserServiceGrpc.UserServiceGrpcBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;

    public GrpcUserServiceImpl(
        IUserService userService,
        IValidator<CreateUserRequest> createUserValidator,
        IValidator<UpdateUserRequest> updateUserValidator)
    {
        _userService = userService;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
    }

    public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var validationResult = _createUserValidator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errorMessages));
        }

        var newUserId = await _userService.CreateUserAsync(request, context.CancellationToken);
        return new CreateUserResponse { Id = newUserId };
    }

    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        var result = await _userService.DeleteUserAsync(request.Id, context.CancellationToken);
        if (!result)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        return new DeleteUserResponse { Result = true };
    }

    public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var validationResult = _updateUserValidator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errorMessages));
        }

        var result = await _userService.UpdateUserAsync(request, context.CancellationToken);
        if (!result)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        return new UpdateUserResponse { Result = true };
    }

    public override async Task<GetUserByIdResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        var user = await _userService.GetUserByIdAsync(request.Id, context.CancellationToken);
        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        var userModel = UserMapper.ToUserModel(user);
        return new GetUserByIdResponse { User = userModel };
    }

    public override async Task<GetUsersByNameResponse> GetUsersByName(GetUsersByNameRequest request, ServerCallContext context)
    {
        var users = await _userService.GetByNameAndSurnameAsync(request.Name, request.Surname, context.CancellationToken);
        var response = new GetUsersByNameResponse();
        if (users.Length > 0)
            response.Users.AddRange(UserMapper.ToUserModels(users));
        return response;
    }
}
