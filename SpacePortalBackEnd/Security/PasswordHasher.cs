using System.Security.Cryptography;

namespace SpacePortalBackEnd.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static (byte[] hash, byte[] salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] salt, byte[] expectedHash)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }
}
