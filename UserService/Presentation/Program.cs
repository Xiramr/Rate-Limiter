using Dapper;
using FluentValidation;
using StackExchange.Redis;
using UserService.Application.Validators;
using UserService.Domain.Configuration;
using UserService.Domain.Interfaces;
using UserService.Grpc.Interceptors;
using UserService.Grpc.Services;
using UserService.Infrastructure.Repositories;
using UserService.Protos;
using UserServiceImpl = UserService.Application.Services.UserService;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.Configure<DbConnectionOptions>(builder.Configuration.GetSection(DbConnectionOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions!.ConnectionString));

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IUserBanRepository, UserBanRepository>();

builder.Services.AddSingleton<IUserService, UserServiceImpl>();
builder.Services.AddSingleton<IUserBanService, UserService.Application.Services.UserBanService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IValidator<CreateUserRequest>, CreateUserValidator>();
builder.Services.AddSingleton<IValidator<UpdateUserRequest>, UpdateUserValidator>();

builder.Services.AddSingleton<ExceptionMappingInterceptor>();

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.Interceptors.Add<ExceptionMappingInterceptor>();
    options.Interceptors.Add<AuthenticationInterceptor>();
    options.Interceptors.Add<RateLimitInterceptor>();
});

var app = builder.Build();

app.MapGrpcService<GrpcUserServiceImpl>();

app.Run();