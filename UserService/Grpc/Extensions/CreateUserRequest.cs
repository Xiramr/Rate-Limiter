using UserService.Domain.Interfaces;

namespace UserService.Protos;

public partial class CreateUserRequest : IUser
{
    int IUser.Id => 0;
}