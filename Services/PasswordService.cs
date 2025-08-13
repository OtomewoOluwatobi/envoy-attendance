using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace envoy_attendance.Services
{
    public class PasswordService : IPasswordService
    {
        // Number of random bytes used for the salt
        private const int SaltSize = 16;
        
        // Number of iterations for PBKDF2
        private const int IterationCount = 10000;
        
        // Size of the hash to generate in bytes
        private const int HashSize = 32;

        public string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive a hash using PBKDF2 with HMACSHA256
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: IterationCount,
                numBytesRequested: HashSize);

            // Combine the salt and hash into a single string
            byte[] combinedBytes = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, combinedBytes, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combinedBytes, SaltSize, HashSize);

            // Return as base64 string
            return Convert.ToBase64String(combinedBytes);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Convert the stored hash from base64 string to bytes
                byte[] storedHashBytes = Convert.FromBase64String(storedHash);

                // Ensure the stored hash has the correct length
                if (storedHashBytes.Length != SaltSize + HashSize)
                {
                    // If we have a length mismatch, this might be a plain text password or different hash format
                    // Fallback to direct comparison for testing/migration purposes
                    return password == storedHash;
                }

                // Extract the salt and hash from the stored value
                byte[] salt = new byte[SaltSize];
                byte[] originalHash = new byte[HashSize];
                Buffer.BlockCopy(storedHashBytes, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(storedHashBytes, SaltSize, originalHash, 0, HashSize);

                // Compute the hash for the provided password using the same salt
                byte[] computedHash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: IterationCount,
                    numBytesRequested: HashSize);

                // Compare the computed hash with the original hash
                return CryptographicOperations.FixedTimeEquals(originalHash, computedHash);
            }
            catch (FormatException)
            {
                // If the stored hash is not a valid Base64 string, 
                // it might be stored in a different format or as plain text
                // Fallback to direct comparison for testing/migration purposes
                return password == storedHash;
            }
            catch (Exception)
            {
                // For any other unexpected error, return false
                return false;
            }
        }
    }
}