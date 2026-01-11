using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.IO;
using System.Security.Cryptography;

namespace HackHelper.Services
{
    public class EncryptionService
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("LauncherLoader_Salt_Key_2024");
        private const int Iterations = 10000;

        public static string Encrypt(string plainText, string password)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Salt, Iterations);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string encryptedText, string password)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            using (Aes aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Salt, Iterations);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}
