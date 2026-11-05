using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Models
{
    /// <summary>
    /// Profil sayfası iki tabloyu birlikte düzenler: ana sayfa (HeroSection) ve iletişim (Contact).
    /// Bir view yalnızca tek bir model alabildiği için ikisini bu sınıfta taşıyoruz.
    /// </summary>
    public class ProfileViewModel
    {
        public HeroSection Hero { get; set; } = new();

        public Contact Contact { get; set; } = new();
    }
}
