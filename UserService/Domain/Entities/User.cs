using UserService.Domain.Interfaces;

namespace UserService.Domain.Entities;

public class User : IUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}