using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class SecurityHelper
    {
        


        private static readonly string KEY = "YourSecretKey123"; // 16 karakterlik anahtar
        private static readonly string IV = "YourSecretIV1234"; // 16 karakterlik IV

        // Mesajı şifrele
        public static string EncryptMessage(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(KEY);
                aes.IV = Encoding.UTF8.GetBytes(IV);

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        // Mesajı çöz
        public static string DecryptMessage(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(KEY);
                aes.IV = Encoding.UTF8.GetBytes(IV);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        // Mesajın hash'ini al
        public static string GetMessageHash(string encryptedMessage)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptedMessage));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

      

    }
  

}
