using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Elwark.Extensions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;

namespace Elwark.People.Api.Infrastructure
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int PasswordLength = 1024;
        private const byte SaltLength = 32;
        private const int Iterations = 10000;
        private readonly byte[] _appHash;

        public PasswordHasher(string hash)
        {
            if (hash.NullIfEmpty() is null) 
                throw new ArgumentNullException(nameof(hash));
            
            _appHash = Encoding.UTF8.GetBytes(hash);
        }
        
        public byte[] CreateSalt() => GenerateRandomBytes(SaltLength);

        public byte[] CreatePasswordHash(string password, byte[] salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            return HashPassword(Encoding.UTF8.GetBytes(password), salt);
        }

        public bool IsEqual(string password, byte[] passwordHash, byte[] salt)
        {
            if (passwordHash is null) 
                throw new ArgumentNullException(nameof(passwordHash));
            
            if (salt is null) 
                throw new ArgumentNullException(nameof(salt));
            
            var hash = CreatePasswordHash(password, salt);
            return hash.SequenceEqual(passwordHash);
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            return bytes;
        }

        private byte[] HashPassword(byte[] password, byte[] salt)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));
            if (password.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(password));
            if (salt.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(salt));

            var saltAndAppHash = new byte[salt.Length + _appHash.Length];
            Buffer.BlockCopy(salt, 0, saltAndAppHash, 0, salt.Length);
            Buffer.BlockCopy(_appHash, 0, saltAndAppHash, salt.Length, _appHash.Length);

            var passwordAndAppHash = new byte[password.Length + _appHash.Length];
            Buffer.BlockCopy(password, 0, passwordAndAppHash, 0, password.Length);
            Buffer.BlockCopy(_appHash, 0, passwordAndAppHash, password.Length, _appHash.Length);

            using var sha = SHA512.Create();
            using var rfc = new Rfc2898DeriveBytes(sha.ComputeHash(passwordAndAppHash), sha.ComputeHash(saltAndAppHash), Iterations);
            return rfc.GetBytes(PasswordLength);
        }
    }
}