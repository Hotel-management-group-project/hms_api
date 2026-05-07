// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HMS.API.Data
{
    /// <summary>
    /// AES-256-CBC value converter for EF Core — encrypts strings before writing to the database
    /// and decrypts them when reading back. The key is derived from the app's DataProtection key
    /// via configuration (DataProtection:EncryptionKey). Values are stored as Base64.
    /// </summary>
    public class EncryptedStringConverter : ValueConverter<string?, string?>
    {
        public EncryptedStringConverter(string base64Key)
            : base(
                v => Encrypt(v, base64Key),
                v => Decrypt(v, base64Key))
        { }

        private static string? Encrypt(string? plainText, string base64Key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var key = Convert.FromBase64String(base64Key);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV (16 bytes) so we can decrypt without storing it separately
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        private static string? Decrypt(string? cipherText, string base64Key)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var key = Convert.FromBase64String(base64Key);
                var fullBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = key;

                // Extract the prepended IV
                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullBytes.Length - iv.Length];
                Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullBytes, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Return the raw value if it was stored unencrypted (e.g. seeded data)
                return cipherText;
            }
        }
    }
}
