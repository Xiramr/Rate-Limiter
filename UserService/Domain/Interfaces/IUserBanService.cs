namespace UserService.Domain.Interfaces;

public interface IUserBanService
{
    Task<bool> IsUserBannedAsync(int userId, string endpoint, CancellationToken cancellationToken);
}