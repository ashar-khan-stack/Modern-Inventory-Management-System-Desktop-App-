using System;
using System.Security.Cryptography;

namespace ModernInventory.Desktop.Infrastructure.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            return (Convert.ToHexString(hashBytes), Convert.ToHexString(saltBytes));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromHexString(storedSalt);
            byte[] calculatedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            byte[] expectedHash = Convert.FromHexString(storedHash);
            return CryptographicOperations.FixedTimeEquals(calculatedHash, expectedHash);
        }
    }
}
