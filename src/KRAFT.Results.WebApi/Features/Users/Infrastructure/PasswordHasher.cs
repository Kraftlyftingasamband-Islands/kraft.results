using System.Security.Cryptography;

namespace KRAFT.Results.WebApi.Features.Users.Infrastructure;

internal static class PasswordHasher
{
    private const char Separator = '.';
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return $"{Convert.ToHexString(salt)}{Separator}{Convert.ToHexString(hash)}";
    }

    public static bool Verify(string password, string hashedPassword)
    {
        string[] parts = hashedPassword.Split(Separator, 2);

        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromHexString(parts[0]);
        byte[] hash = Convert.FromHexString(parts[1]);
        byte[] input = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, hash.Length);

        return CryptographicOperations.FixedTimeEquals(hash, input);
    }
}