using StudentInformationSystem.Models;
using System;
using System.Security.Cryptography;

namespace StudentInformationSystem.Helpers
{
    public static class PasswordSecurity
    {
        private const string Scheme = "PBKDF2";
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static bool IsHashed(string password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.StartsWith(Scheme + "$", StringComparison.Ordinal);
        }

        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(plainPassword));
            }

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var deriveBytes = new Rfc2898DeriveBytes(plainPassword, salt, Iterations))
            {
                hash = deriveBytes.GetBytes(HashSize);
            }

            return string.Format(
                "{0}${1}${2}${3}",
                Scheme,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string plainPassword, string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedPassword))
            {
                return false;
            }

            if (!IsHashed(storedPassword))
            {
                return string.Equals(plainPassword, storedPassword, StringComparison.Ordinal);
            }

            var parts = storedPassword.Split('$');
            if (parts.Length != 4 || !string.Equals(parts[0], Scheme, StringComparison.Ordinal))
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], out iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(plainPassword, salt, iterations))
            {
                var actualHash = deriveBytes.GetBytes(expectedHash.Length);
                return FixedTimeEquals(actualHash, expectedHash);
            }
        }

        public static bool VerifyAndUpgrade(Users user, string plainPassword, out bool upgraded)
        {
            upgraded = false;

            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return false;
            }

            if (IsHashed(user.Password))
            {
                return VerifyPassword(plainPassword, user.Password);
            }

            if (!string.Equals(user.Password, plainPassword, StringComparison.Ordinal))
            {
                return false;
            }

            user.Password = HashPassword(plainPassword);
            upgraded = true;
            return true;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
