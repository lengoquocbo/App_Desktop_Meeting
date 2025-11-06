using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Online_Meeting.Client.Services
{
    public class TokenStorageService
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OnlineMeeting",
            "token.dat"
        );

        private static readonly byte[] Key = Encoding.UTF8.GetBytes("OnlineMeetingSecretKey1234567890"); // 32 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("InitVector123456"); // 16 bytes

        public static void SaveToken(string token, string refreshToken = null)
        {
            try
            {
                var tokenData = new TokenData
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(tokenData);
                var encryptedData = Encrypt(json);

                // Tạo thư mục nếu chưa có
                var directory = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(TokenFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving token: {ex.Message}");
            }
        }

        public static string GetToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return null;

                var encryptedData = File.ReadAllText(TokenFilePath);
                var json = Decrypt(encryptedData);
                var tokenData = JsonSerializer.Deserialize<TokenData>(json);

                return tokenData?.Token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting token: {ex.Message}");
                return null;
            }
        }

        public static string GetRefreshToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return null;

                var encryptedData = File.ReadAllText(TokenFilePath);
                var json = Decrypt(encryptedData);
                var tokenData = JsonSerializer.Deserialize<TokenData>(json);

                return tokenData?.RefreshToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting refresh token: {ex.Message}");
                return null;
            }
        }

        public static void ClearToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing token: {ex.Message}");
            }
        }

        private static string Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private static string Decrypt(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        private class TokenData
        {
            public string Token { get; set; }
            public string RefreshToken { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}