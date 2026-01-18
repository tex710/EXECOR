using System;
using System.Security.Cryptography;
using System.Text;

namespace Execor.Services
{
    public class EncryptionService
    {
        // Optional: Additional entropy can be added for enhanced security.
        // This entropy would need to be stored securely and be available for decryption.
        // For simplicity, we are not using it here, but it's an option for higher security needs.
        private static readonly byte[] Entropy = null; // e.g., Encoding.UTF8.GetBytes("Your-Optional-Entropy-String");

        /// <summary>
        /// Encrypts a string using the Windows Data Protection API (DPAPI) for the current user.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <returns>A Base64 encoded string representing the encrypted data.</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                
                // Use ProtectedData to encrypt the data.
                // The scope is set to CurrentUser, meaning only the current user on the current machine can decrypt it.
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (CryptographicException)
            {
                // Handle exceptions, e.g., if the DPAPI store is unavailable.
                // Depending on the application's needs, you might want to log this or alert the user.
                return string.Empty; 
            }
        }

        /// <summary>
        /// Decrypts a string using the Windows Data Protection API (DPAPI).
        /// </summary>
        /// <param name="encryptedText">The Base64 encoded encrypted text.</param>
        /// <returns>The decrypted plaintext string.</returns>
        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Use ProtectedData to decrypt the data.
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                // This can happen if the data is corrupt, from another user/machine, or if the user's profile is corrupted.
                return string.Empty;
            }
            catch (FormatException)
            {
                // This can happen if the base64 string is not valid.
                return string.Empty;
            }
        }
    }
}
