using System.Security.Cryptography;
using System.Text;

namespace PortfolioApp.Data.Seeders
{
    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            var combined = Encoding.UTF8.GetBytes(password + salt);
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(combined));
        }

        public static bool Verify(string inputPassword, string storedHash, string storedSalt)
        {
            var hash = HashPassword(inputPassword, storedSalt);
            return hash == storedHash;
        }
    }

}
