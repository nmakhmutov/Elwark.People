using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Password;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int PasswordLength = 512;
    private const byte SaltLength = 32;
    private const int Iterations = 1000;
    private readonly byte[] _appHash;

    public PasswordHasher(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Value cannot be null or empty.", nameof(hash));

        _appHash = Encoding.UTF8.GetBytes(hash);
    }

    public byte[] CreateSalt()
    {
        var bytes = new byte[SaltLength];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return bytes;
    }

    public byte[] CreateHash(string password, byte[] salt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        return KeyDerivation.Pbkdf2(
            prf: KeyDerivationPrf.HMACSHA256,
            password: password,
            salt: salt.Concat(_appHash).ToArray(),
            iterationCount: Iterations,
            numBytesRequested: PasswordLength
        );
    }
}
