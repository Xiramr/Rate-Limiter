using UserService.Domain.Interfaces;

namespace UserService.Protos;

public partial class UpdateUserRequest : IUser
{
    string IUser.Login => string.Empty;
}