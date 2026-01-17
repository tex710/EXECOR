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
        private static readonly string AccountsFile = Path.Combine(PathManager.AppDataFolder, "steam_accounts.dat");


        private List<SteamAccount> _accounts = new List<SteamAccount>();

        public SteamAccountManager()
        {
            LoadAccounts();
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

                var encryptedData = EncryptionService.Encrypt(json);
                File.WriteAllText(AccountsFile, encryptedData);
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

                var encryptedData = File.ReadAllText(AccountsFile);
                if (string.IsNullOrEmpty(encryptedData))
                {
                    _accounts = new List<SteamAccount>();
                    return;
                }
                
                var json = EncryptionService.Decrypt(encryptedData);
                _accounts = JsonSerializer.Deserialize<List<SteamAccount>>(json) ?? new List<SteamAccount>();
            }
            catch (Exception ex)
            {
                // If decryption fails or file is corrupted, start with empty list
                _accounts = new List<SteamAccount>();
                Console.WriteLine($"Failed to load accounts: {ex.Message}");
            }
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

        public void TogglePin(string id)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == id);
            if (acc == null) return;

            acc.IsPinned = !acc.IsPinned;
            SaveAccounts();
        }

    }
}