using System;
using System.Security.Cryptography;
using System.Text;
using Elwark.Extensions;
using Elwark.People.Shared;
using Newtonsoft.Json;

namespace Elwark.People.Api.Infrastructure.Security
{
    public class DataEncryption : IDataEncryption
    {
        private static readonly RijndaelManaged Rijndael = new RijndaelManaged();

        public DataEncryption(string key, string iv)
        {
            if (key.NullIfEmpty() is null)
                throw new ArgumentNullException(nameof(key));

            if (iv.NullIfEmpty() is null)
                throw new ArgumentNullException(nameof(iv));

            Rijndael.Mode = CipherMode.CBC;
            Rijndael.Padding = PaddingMode.PKCS7;

            using DeriveBytes rgb = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(iv));

            Rijndael.Key = rgb.GetBytes(Rijndael.KeySize >> 3);
            Rijndael.IV = rgb.GetBytes(Rijndael.BlockSize >> 3);
        }

        public string EncryptToString<T>(T model)
        {
            var bytes = EncryptToBytes(model);

            return Convert.ToBase64String(bytes);
        }

        public T DecryptFromString<T>(string cipher)
        {
            if (cipher is null)
                throw new ArgumentNullException(nameof(cipher));

            var base64 = Convert.FromBase64String(cipher);

            return DecryptFromBytes<T>(base64);
        }

        public byte[] EncryptToBytes<T>(T model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            var json = JsonConvert.SerializeObject(model, ElwarkJsonSettings.Value);

            using var transform = Rijndael.CreateEncryptor();
            var cipher = Encoding.UTF8.GetBytes(json);

            return transform.TransformFinalBlock(cipher, 0, cipher.Length);
        }

        public T DecryptFromBytes<T>(byte[] cipher)
        {
            if (cipher is null)
                throw new ArgumentNullException(nameof(cipher));

            if (cipher.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(cipher));

            var transform = Rijndael.CreateDecryptor();
            var decryptedValue = transform.TransformFinalBlock(cipher, 0, cipher.Length);
            var json = Encoding.UTF8.GetString(decryptedValue);

            return JsonConvert.DeserializeObject<T>(json, ElwarkJsonSettings.Value)
                   ?? throw new InvalidOperationException("Value cannot be null");
        }
    }
}