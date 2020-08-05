using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Elwark.People.Api.Extensions
{
    public static class StringExtensions
    {
        public static string ToMd5Hash(this string value)
        {
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5Provider.ComputeHash(Encoding.UTF8.GetBytes(value));

            return string.Join(string.Empty, bytes.Select(x => x.ToString("x2")));
        }
    }
}