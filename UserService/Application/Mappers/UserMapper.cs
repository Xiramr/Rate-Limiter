using Riok.Mapperly.Abstractions;
using UserService.Domain.Interfaces;
using UserService.Protos;

namespace UserService.Application.Mappers;

[Mapper]
public static partial class UserMapper
{
    [MapperIgnoreSource(nameof(IUser.Password))] 
    public static partial UserModel ToUserModel(IUser user);
    public static partial IEnumerable<UserModel> ToUserModels(IEnumerable<IUser> users);
}