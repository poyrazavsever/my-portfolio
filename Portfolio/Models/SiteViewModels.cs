using Portfolio.Entities;

namespace Portfolio.Models
{
    /// <summary>&lt;head&gt; içindeki başlık ve arama motoru/paylaşım açıklaması.</summary>
    public record HeadViewModel(string Title, string? Description);

    /// <summary>Section başlığı: hareketli cam ikon + başlık + kısa açıklama. (_SectionHead partial'ı)</summary>
    public record SectionHeadModel(string Icon, string Title, string? Lead = null);

    /// <summary>Navbar ve footer'ın ortak ihtiyacı: isim, sosyal bağlantılar, CV var mı.</summary>
    public class SiteChromeViewModel
    {
        public string Name { get; init; } = "Poyraz Avsever";
        public string? Email { get; init; }
        public string? Linkedin { get; init; }
        public string? Github { get; init; }
        public bool HasCv { get; init; }
    }

    /// <summary>Hero, dört ayrı tablodan beslenir: HeroSection, About, Contact, SiteSetting.</summary>
    public class HeroViewModel
    {
        public string Name { get; init; } = "Poyraz Avsever";
        public string? Role { get; init; }
        public string? Intro { get; init; }
        public string? ImageUrl { get; init; }
        public string? Location { get; init; }
        public bool OpenToWork { get; init; }
        public string? Email { get; init; }
        public string? Linkedin { get; init; }
        public string? Github { get; init; }
        public bool HasCv { get; init; }
    }

    public class ProjectSectionViewModel
    {
        public List<Project> Projects { get; init; } = [];

        /// <summary>Yalnızca aktif projesi olan kategoriler (boş filtre butonu göstermemek için).</summary>
        public List<Category> Categories { get; init; } = [];
    }

    public class ContactSectionViewModel
    {
        public Contact Info { get; init; } = new();

        /// <summary>Form verisi. Doğrulama hatasıyla geri dönüldüğünde kullanıcının yazdıkları burada korunur.</summary>
        public Message Form { get; init; } = new();
    }
}
