using System.Security.Cryptography;

using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed class Password : ValueObject<string>
{
    private const char Separator = '.';
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private Password(string password)
        : base(password)
    {
    }

    internal static Password Parse(string value) => new(value);

    internal static Result<Password> Hash(string value)
    {
        if (Validate(value) is { IsFailure: true } validationResult)
        {
            return validationResult.Error;
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, Algorithm, HashSize);

        string password = $"{Convert.ToHexString(salt)}{Separator}{Convert.ToHexString(hash)}";

        return new Password(password);
    }

    internal static Result Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new Error("Password.Empty", "Password cannot be empty");
        }

        return Result.Success();
    }

    internal bool Verify(string unhashedPassword)
    {
        string[] parts = Value.Split(Separator, 2);

        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromHexString(parts[0]);
        byte[] hash = Convert.FromHexString(parts[1]);
        byte[] input = Rfc2898DeriveBytes.Pbkdf2(unhashedPassword, salt, Iterations, Algorithm, hash.Length);

        return CryptographicOperations.FixedTimeEquals(hash, input);
    }
}