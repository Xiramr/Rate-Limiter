using System.Security.Cryptography;

namespace UserService.Application.Security;

public static class PasswordHasher
{
    private const string Prefix = "PBKDF2$SHA256$";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 120000;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password)) 
            throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }
}