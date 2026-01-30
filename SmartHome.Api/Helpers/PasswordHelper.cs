using System.Security.Cryptography;
using System.Text;

namespace SmartHome.Api.Helpers
{
    public static class PasswordHelper
    {
        // Hash mật khẩu
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        // Verify mật khẩu
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashInput = HashPassword(password);
            return hashInput == hashedPassword;
        }
    }
}
