using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Services
{
    /// <summary>
    /// Tek yöneticili giriş için ayarlar. Şifrenin kendisi değil, PBKDF2 hash'i saklanır.
    /// Üretimde ortam değişkenleriyle verilir:  AdminAuth__Username, AdminAuth__PasswordHash
    /// Hash üretmek için:  dotnet run -- --hash-password
    /// </summary>
    public class AdminAuthOptions
    {
        public const string Section = "AdminAuth";

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(PasswordHash);
    }

    /// <summary>
    /// PBKDF2 (SHA-256) ile şifre hash'leme. Biçim: PBKDF2:iterasyon:tuz(base64):hash(base64)
    /// (Ayraç ":" — "$" olsaydı Docker Compose .env dosyasında değişken sanıp bozardı.)
    /// Neden düz SHA-256 değil? Hızlı hash'ler saniyede milyarlarca deneme yapılmasına izin verir;
    /// PBKDF2 binlerce tur uygulayarak her denemeyi kasıtlı olarak yavaşlatır. Tuz (salt) da aynı şifrenin
    /// her seferinde farklı hash üretmesini sağlar, hazır tablolarla (rainbow table) kırılmayı engeller.
    /// </summary>
    public static class AdminPasswordHasher
    {
        private const int Iterations = 210_000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
            return $"PBKDF2:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split(':');
            if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations))
                return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Sabit zamanlı karşılaştırma: hangi baytta farklılaştığı süreden anlaşılamasın.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        /// <summary>Kullanıcı adını da sabit zamanda karşılaştır (geçerli kullanıcı adı tahmin edilemesin).</summary>
        public static bool UsernameMatches(string given, string expected) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(given.Trim().ToLowerInvariant()),
                Encoding.UTF8.GetBytes(expected.Trim().ToLowerInvariant()));
    }
}
