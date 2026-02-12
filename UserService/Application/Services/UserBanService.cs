using UserService.Domain.Interfaces;

namespace UserService.Application.Services;

public class UserBanService : IUserBanService
{
    private readonly IUserBanRepository _userBanRepository;

    public UserBanService(IUserBanRepository userBanRepository)
    {
        _userBanRepository = userBanRepository;
    }

    public Task<bool> IsUserBannedAsync(int userId, string endpoint, CancellationToken cancellationToken)
    {
        return _userBanRepository.IsUserBannedAsync(userId, endpoint, cancellationToken);
    }
}