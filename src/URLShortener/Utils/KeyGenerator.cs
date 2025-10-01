using System.Security.Cryptography;
using System.Text;

namespace URLShortener.Utils
{
    public static class ShortKeyGenerator
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static string Generate(int len = 7)
        {
            var bytes = RandomNumberGenerator.GetBytes(len);
            var sb = new StringBuilder(len);
            for (int i = 0; i < len; i++) sb.Append(Alphabet[bytes[i] % Alphabet.Length]);
            return sb.ToString();
        }
    }

}
