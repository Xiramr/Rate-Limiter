namespace UserService.Domain.Interfaces;

public interface IUserBanRepository
{
    Task<bool> IsUserBannedAsync(int userId, string endpoint, CancellationToken cancellationToken);
}