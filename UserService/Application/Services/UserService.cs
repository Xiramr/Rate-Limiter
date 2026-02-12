using Microsoft.Extensions.Caching.Memory;
using UserService.Application.Security;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services;

public class UserService : IUserService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;

    public UserService(IUserRepository userRepository, IMemoryCache cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<int> CreateUserAsync(IUser user, CancellationToken cancellationToken)
    {
        var toCreate = new User
        {
            Login = user.Login?.Trim() ?? string.Empty,
            Password = PasswordHasher.Hash(user.Password ?? string.Empty),
            Name = user.Name?.Trim() ?? string.Empty,
            Surname = user.Surname?.Trim() ?? string.Empty,
            Age = user.Age
        };

        var newId = await _userRepository.CreateUserAsync(toCreate, cancellationToken);
        if (newId == 0) throw new UserAlreadyExistsException("User with this login already exists");

        var created = await _userRepository.GetByIdAsync(newId, cancellationToken);
        if (created != null)
        {
            _cache.Set(GetUserCacheKey(newId), created, CacheTtl);
            _cache.Remove(GetUsersByCacheKey(created.Name, created.Surname));
        }

        return newId;
    }

    public async Task<IUser?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = GetUserCacheKey(id);

        if (_cache.TryGetValue(cacheKey, out IUser? cachedUser))
            return cachedUser;

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user != null)
            _cache.Set(cacheKey, user, CacheTtl);

        return user;
    }

    public async Task<IUser[]> GetByNameAndSurnameAsync(string name, string surname, CancellationToken cancellationToken)
    {
        var cacheKey = GetUsersByCacheKey(name, surname);

        if (_cache.TryGetValue(cacheKey, out IUser[]? cachedUsers))
            return cachedUsers!;

        var result = await _userRepository.GetByNameAndSurnameAsync(name, surname, cancellationToken);

        if (result.Length > 0)
            _cache.Set(cacheKey, result, CacheTtl);

        return result;
    }

    public async Task<bool> UpdateUserAsync(IUser user, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByIdAsync(user.Id, cancellationToken);
        if (existing == null) return false;

        var toUpdate = new User
        {
            Id = user.Id,
            Login = existing.Login,
            Password = PasswordHasher.HashIfNeeded(user.Password ?? string.Empty),
            Name = user.Name?.Trim() ?? string.Empty,
            Surname = user.Surname?.Trim() ?? string.Empty,
            Age = user.Age
        };

        var updatedOk = await _userRepository.UpdateUserAsync(toUpdate, cancellationToken);
        if (!updatedOk) return false;

        _cache.Remove(GetUserCacheKey(user.Id));
        _cache.Remove(GetUsersByCacheKey(existing.Name, existing.Surname));

        var updated = await _userRepository.GetByIdAsync(user.Id, cancellationToken);
        if (updated != null)
        {
            _cache.Set(GetUserCacheKey(user.Id), updated, CacheTtl);
            _cache.Remove(GetUsersByCacheKey(updated.Name, updated.Surname));
        }
        else
        {
            _cache.Remove(GetUsersByCacheKey(toUpdate.Name, toUpdate.Surname));
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken)
    {
        var userKey = GetUserCacheKey(id);

        _cache.TryGetValue(userKey, out IUser? cachedUser);
        var existing = cachedUser ?? await _userRepository.GetByIdAsync(id, cancellationToken);

        var deletedOk = await _userRepository.DeleteUserAsync(id, cancellationToken);
        if (!deletedOk) return false;

        _cache.Remove(userKey);

        if (existing != null)
            _cache.Remove(GetUsersByCacheKey(existing.Name, existing.Surname));

        return true;
    }

    private static string GetUserCacheKey(int id) => $"user:{id}";

    private static string GetUsersByCacheKey(string name, string surname)
        => $"users:{NormalizeKeyPart(name)}:{NormalizeKeyPart(surname)}";

    private static string NormalizeKeyPart(string s)
        => (s ?? string.Empty).Trim().ToLowerInvariant();
}
