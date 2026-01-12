using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HackHelper.Models;

namespace HackHelper.Services
{
    public class SteamAccountManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EXECOR"
        );
        private static readonly string AccountsFile = Path.Combine(AppDataFolder, "steam_accounts.dat");
        // Create a 32-byte key by padding/truncating
        private static readonly byte[] EncryptionKey = GetEncryptionKey();

        private static byte[] GetEncryptionKey()
        {
            var keyString = "EXECOR_STEAM_KEY";
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            var key = new byte[32]; // AES-256 requires exactly 32 bytes
            Array.Copy(keyBytes, key, Math.Min(keyBytes.Length, 32));
            // Fill remaining bytes with a pattern if needed
            for (int i = keyBytes.Length; i < 32; i++)
            {
                key[i] = (byte)(i * 7); // Simple pattern to fill remaining bytes
            }
            return key;
        }

        private List<SteamAccount> _accounts = new List<SteamAccount>();

        public SteamAccountManager()
        {
            EnsureAppDataFolderExists();
            LoadAccounts();
        }

        private void EnsureAppDataFolderExists()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }

        // Get all accounts
        public List<SteamAccount> GetAllAccounts()
        {
            return _accounts.OrderByDescending(a => a.LastUsed ?? DateTime.MinValue).ToList();
        }

        // Add new account
        public void AddAccount(SteamAccount account)
        {
            if (_accounts.Any(a => a.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"An account with username '{account.Username}' already exists.");
            }

            _accounts.Add(account);
            SaveAccounts();
        }

        // Update existing account
        public void UpdateAccount(SteamAccount account)
        {
            var existing = _accounts.FirstOrDefault(a => a.Id == account.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            var index = _accounts.IndexOf(existing);
            _accounts[index] = account;
            SaveAccounts();
        }

        // Delete account
        public void DeleteAccount(string accountId)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (account != null)
            {
                _accounts.Remove(account);
                SaveAccounts();
            }
        }

        // Get account by ID
        public SteamAccount? GetAccountById(string accountId)
        {
            return _accounts.FirstOrDefault(a => a.Id == accountId);
        }

        // Update last used time and increment login count
        public void RecordAccountUsage(string accountId)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (account != null)
            {
                account.LastUsed = DateTime.Now;
                account.LoginCount++;
                SaveAccounts();
            }
        }

        // Save accounts to encrypted file
        private void SaveAccounts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var encryptedData = EncryptString(json);
                File.WriteAllBytes(AccountsFile, encryptedData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save accounts: {ex.Message}", ex);
            }
        }

        // Load accounts from encrypted file
        private void LoadAccounts()
        {
            try
            {
                if (!File.Exists(AccountsFile))
                {
                    _accounts = new List<SteamAccount>();
                    return;
                }

                var encryptedData = File.ReadAllBytes(AccountsFile);
                var json = DecryptString(encryptedData);
                _accounts = JsonSerializer.Deserialize<List<SteamAccount>>(json) ?? new List<SteamAccount>();
            }
            catch (Exception ex)
            {
                // If decryption fails or file is corrupted, start with empty list
                _accounts = new List<SteamAccount>();
                Console.WriteLine($"Failed to load accounts: {ex.Message}");
            }
        }

        // Encrypt string using AES
        private byte[] EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();

            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return msEncrypt.ToArray();
        }

        // Decrypt string using AES
        private string DecryptString(byte[] cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;

            // Read IV from the beginning of the stream
            var iv = new byte[aes.IV.Length];
            Array.Copy(cipherText, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        // Export accounts (for backup)
        public void ExportAccounts(string filePath)
        {
            var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }

        // Import accounts (from backup)
        public void ImportAccounts(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var importedAccounts = JsonSerializer.Deserialize<List<SteamAccount>>(json);

            if (importedAccounts != null)
            {
                foreach (var account in importedAccounts)
                {
                    if (!_accounts.Any(a => a.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase)))
                    {
                        _accounts.Add(account);
                    }
                }
                SaveAccounts();
            }
        }
    }
}