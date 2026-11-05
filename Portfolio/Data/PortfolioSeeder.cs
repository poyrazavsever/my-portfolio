using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Entities;

namespace Portfolio.Data
{
    /// <summary>
    /// Portfolyonun gerçek içeriği (kaynak: poyrazavsever.com, Kasım 2026).
    /// Çalıştırma:  dotnet run -- --seed
    ///
    /// İçerik tablolarını (eğitim, deneyim, yetenek, sertifika, kategori, proje) silip yeniden doldurur;
    /// tek satırlık tabloları (hero, hakkımda, iletişim, ayarlar) günceller.
    /// Gelen mesajlara ve yüklenen CV'ye DOKUNMAZ.
    /// </summary>
    public static class PortfolioSeeder
    {
        private const string Email = "poyrazavsever@gmail.com";
        private const string Location = "Ankara, Türkiye";
        private const string Github = "https://github.com/poyrazavsever";
        private const string Linkedin = "https://www.linkedin.com/in/poyrazavsever/";

        public static async Task SeedAsync(PortfolioContext context)
        {
            // Tek bir transaction: yarıda hata olursa DB eski haline döner.
            await using var transaction = await context.Database.BeginTransactionAsync();

            await ClearContentAsync(context);
            await UpsertProfileAsync(context);

            context.Educations.AddRange(Educations());
            context.Experiences.AddRange(Experiences());
            context.Skills.AddRange(Skills());
            context.Certificates.AddRange(Certificates());

            var categories = new Dictionary<string, Category>
            {
                ["work"] = new() { CategoryName = "İş Projeleri" },
                ["products"] = new() { CategoryName = "Ürünler" },
                ["extensions"] = new() { CategoryName = "Eklentiler" },
            };
            context.Categories.AddRange(categories.Values);
            context.Projects.AddRange(Projects(categories));

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private static async Task ClearContentAsync(PortfolioContext context)
        {
            // Sıra önemli: Project -> Category ilişkisi Restrict, önce projeler silinmeli.
            // ExecuteDeleteAsync: kayıtları belleğe çekmeden tek bir DELETE sorgusu çalıştırır.
            await context.Projects.ExecuteDeleteAsync();
            await context.Categories.ExecuteDeleteAsync();
            await context.Experiences.ExecuteDeleteAsync();
            await context.Educations.ExecuteDeleteAsync();
            await context.Skills.ExecuteDeleteAsync();
            await context.Certificates.ExecuteDeleteAsync();
        }

        private static async Task UpsertProfileAsync(PortfolioContext context)
        {
            var hero = await context.HeroSections.FirstOrDefaultAsync() ?? context.HeroSections.Add(new HeroSection()).Entity;
            hero.Title = "Poyraz Avsever";
            hero.SubTitle = "Yazılım Mühendisliği öğrencisi · Fullstack Developer";
            hero.Description = "Modern web ürünleri, kullanıcı odaklı arayüzler ve üretime hazır yazılım deneyimleri geliştiriyorum.";
            hero.ImageUrl = "/assets/profile/portrait.webp";

            var about = await context.Abouts.FirstOrDefaultAsync() ?? context.Abouts.Add(new About()).Entity;
            about.NameSurname = "Poyraz Avsever";
            about.Expert = "Fullstack web ve mobil geliştirme";
            about.Location = Location;
            about.Email = Email;
            about.OpenToWork = true;
            about.Description =
                "Ortaokulda yazılımla tanıştığımdan beri sürekli öğrenmeye ve kendimi geliştirmeye çalışıyorum.\n" +
                "Lise yıllarında öğrenme hevesim daha da arttı ve web geliştirme tarafında farklı teknolojileri keşfetme fırsatı buldum. " +
                "Lisenin sonlarına doğru mobil dünyasına da adım attım.\n" +
                "Şu an aktif olarak üretiyor, geliştiriyor ve en önemlisi hâlâ öğrenmeye devam ediyorum.";

            var contact = await context.Contacts.FirstOrDefaultAsync() ?? context.Contacts.Add(new Contact()).Entity;
            contact.Description = "Bir fikrin mi var? Birlikte üretelim.";
            contact.SubDescription = "Proje, iş birliği veya içerik fikrin için doğrudan iletişime geç.";
            contact.Email = Email;
            contact.Location = Location;
            contact.Github = Github;
            contact.Linkedin = Linkedin;

            // CV alanlarına dokunmadan yalnızca başlığı ayarla.
            var setting = await context.SiteSettings.FirstOrDefaultAsync() ?? context.SiteSettings.Add(new SiteSetting()).Entity;
            setting.SiteTitle = "Poyraz Avsever · Fullstack Developer";
        }

        private static DateOnly Month(int year, int month) => new(year, month, 1);

        private static IEnumerable<Education> Educations() =>
        [
            new() { EducationTitle = "OSTİM Teknik Üniversitesi", SubTitle = "Yazılım Mühendisliği (3. sınıf)", StartDate = Month(2024, 9), IsActive = true },
            new() { EducationTitle = "Tuşba Fen Lisesi", SubTitle = "Lise", StartDate = Month(2020, 9), EndDate = Month(2024, 6), IsActive = true },
        ];

        private static IEnumerable<Experience> Experiences() =>
        [
            new() { Title = "Fullstack Developer", CompanyName = "Omedya Bilişim A.Ş.", WorkType = "Yarı zamanlı", StartDate = Month(2025, 8), IsActive = true },
            new() { Title = "Fullstack Developer Stajyeri", CompanyName = "Omedya Bilişim A.Ş.", WorkType = "Staj", StartDate = Month(2025, 6), EndDate = Month(2025, 8), IsActive = true },
            new() { Title = "Fullstack Developer Stajyeri", CompanyName = "ARC Foreign Trade", WorkType = "Staj", StartDate = Month(2025, 2), EndDate = Month(2025, 6), IsActive = true },
            new() { Title = "Fullstack Developer Stajyeri", CompanyName = "Tarvina Software Technologies", WorkType = "Staj", StartDate = Month(2024, 10), EndDate = Month(2024, 12), IsActive = true },
        ];

        // Teknoloji listesi siteden alındı; seviyeler öz değerlendirme içindir ve admin panelinden güncellenmelidir.
        private static IEnumerable<Skill> Skills()
        {
            (string Name, int Level)[] items =
            [
                ("TypeScript / JavaScript", 85),
                ("React.js & Next.js", 85),
                ("Node.js & Nest.js", 75),
                ("React Native", 70),
                ("C# & ASP.NET Core", 65),
                ("SQL Server & PostgreSQL", 70),
                ("MongoDB", 65),
                ("Tailwind CSS & Bootstrap", 85),
                ("Git & GitHub", 80),
                ("Figma & tasarım sistemleri", 70),
            ];

            return items.Select((item, index) => new Skill
            {
                Name = item.Name,
                Level = item.Level,
                DisplayOrder = index + 1,
                IsActive = true,
            });
        }

        private static IEnumerable<Certificate> Certificates()
        {
            (string Title, string Issuer, int Year, int Month)[] items =
            [
                ("Scrimba ile JavaScript'in Zor Kısımları", "Scrimba", 2026, 7),
                ("Scrimba ile JavaScript Öğrenimi", "Scrimba", 2026, 7),
                ("Scrimba ile CSS Animasyonları Öğrenimi", "Scrimba", 2026, 7),
                ("Scrimba ile CSS Grid Öğrenimi", "Scrimba", 2026, 7),
                ("Scrimba ile HTML ve CSS Öğrenimi", "Scrimba", 2026, 7),
                ("HTML 101", "Turkcell Geleceği Yazanlar", 2026, 6),
                ("HTML CSS 201", "Turkcell Geleceği Yazanlar", 2026, 6),
                ("JavaScript 301", "Turkcell Geleceği Yazanlar", 2026, 6),
                ("React Native | Uygulamalı React Native Eğitimi", "Udemy", 2025, 6),
                ("GDG Hackathon Katılım Sertifikası", "Google Developer Group", 2025, 5),
                ("Ulusal Öğrenci Kongresi Katılım Sertifikası", "OSTİM Teknik Üniversitesi, İşletme Fakültesi", 2025, 4),
                ("HSD Core Team Katılım Sertifikası", "Huawei Student Developers", 2025, 1),
                ("Node.js ile Web Programcılığı", "BTK Akademi", 2024, 11),
                ("Etkili LinkedIn Profili Oluşturma", "BTK Akademi", 2024, 10),
                ("Versiyon Kontrol: Git & GitHub", "BTK Akademi", 2024, 9),
                ("Girişimcilikte Sunum Teknikleri ve İletişim Yönetimi", "BTK Akademi", 2024, 9),
                ("REACT ile Web Programcılığı", "BTK Akademi", 2024, 6),
                ("Yeşilay Türkiye Münazara Şampiyonası", "Yeşilay", 2023, 7),
                ("Yeşilay Bölge Münazara Turnuvası Şampiyonası", "Yeşilay", 2023, 5),
                ("Python | Sıfırdan İleri Seviye Programlama", "Udemy", 2021, 11),
            ];

            return items.Select(item => new Certificate
            {
                Title = item.Title,
                Issuer = item.Issuer,
                IssueDate = Month(item.Year, item.Month),
                IsActive = true,
            });
        }

        private static IEnumerable<Project> Projects(Dictionary<string, Category> categories)
        {
            const string site = "https://www.poyrazavsever.com/tr/projects/";
            const string go = "https://go.poyrazavsever.com/";

            // Sıralama: listede üstte olan proje sitede önce görünür (CreatedDate'i buna göre veriyoruz).
            (string Category, string Title, string Description, string Url)[] items =
            [
                ("products", "Take Neta", "Freelancer'lar için görev, proje, müşteri, finans, yapay zekâ ve müşteri portalı akışlarını birleştiren self-hosted işletim sistemi.", site + "take-neta"),
                ("products", "Clevio Kids", "Omedya bünyesinde geliştirdiğim; reklamsız oyunları, görevleri ve bilişsel beceri sinyallerini tek akışta birleştirerek ebeveynlerin çocuğunun gelişimini bağlamıyla takip etmesini sağlayan çocuk uygulaması.", site + "clevio-kids"),
                ("work", "OSTİM Web Portalı", "OSTİM Organize Sanayi Bölgesi ve yedi kümesi için geliştirdiğimiz kapsamlı kurumsal web portalı.", site + "ostim-web-portali"),
                ("work", "OSTİM İstihdam", "İŞKUR ile senkron çalışan, adayları iş ve staj fırsatlarıyla buluşturan istihdam portalı.", site + "ostim-istihdam"),
                ("products", "Commit Kingdom", "Yazdığın kodu doğrulayıp enerjiye dönüştüren, bu enerjiyle kuleni büyütüp ordunu kurduğun ve ortak haritada sefere çıktığın oyunlaştırılmış kodlama takipçisi.", site + "commit-kingdom"),
                ("products", "Tudi Daily", "Ajanda, kişisel defterler ve serbest panoyu tek çalışma alanında birleştiren, tablet kalemi odaklı dijital kırtasiye ürünü. Proje yönetimi, review süreçleri ve görev dağılımını ağırlıklı olarak üstlendim.", site + "tudi-daily"),
                ("work", "OSTİM Dış Ticaret", "Dış ticaret yapan firmaları yabancı dil bilen öğrencilerle buluşturmaya odaklanan portal.", go + "ostim-foreign-trade-project-website"),
                ("work", "ARC Foreign Trade", "Ankara merkezli, ihracat odaklı bir üretici firma için kurumsal web sitesi yenileme projesi.", go + "arc-foreign-trade-project-website"),
                ("products", "Targiz App", "Üç kişilik ekipte yazılım liderliği ve full-stack geliştirme sorumluluğunu üstlendiğim; üreticilere hastalık tespiti, lojistik ve pazar erişimi sunan mobil öncelikli tarım platformu.", site + "targiz"),
                ("products", "OhHike", "Masa başında çalışan geliştiriciler için aktivite, haftalık sağlık planı, beslenme ve kalori takibini bir araya getiren self-hosted uygulama.", site + "ohhike"),
                ("products", "Atatürk Kronolojisi", "Açık kaynak katkıcısı olarak yer aldığım; Atatürk'ün hayatındaki önemli olayları, konuşmaları ve reformları etkileşimli bir zaman çizelgesiyle sunan web deneyimi.", go + "ataturk-chronology-project-website"),
                ("extensions", "Mockup Factory", "Görsellerin cihaz mockup'larına saniyeler içinde dönüştürüldüğü, tamamen tarayıcı üzerinde çalışan açık kaynak araç.", go + "mockup-factory-project-website"),
                ("extensions", "Shortcut Injector", "Özel klavye kısayollarıyla önceden tanımlanmış kişisel verileri ve bağlantıları web formlarına hızlıca yerleştiren çapraz tarayıcı eklentisi.", go + "shortcut-injector-project-website"),
                ("extensions", "Tab Audio Relay", "Sekmeler arasındaki ses çalmayı senkronize eden eklenti: eğitim videonuz durunca müziğinizi oynatır, eğitime dönünce müziği duraklatır.", go + "tab-audio-relay-project-website"),
            ];

            var newest = new DateTime(2026, 11, 1);
            return items.Select((item, index) => new Project
            {
                Title = item.Title,
                Description = item.Description,
                GithubUrl = item.Url,
                Category = categories[item.Category],
                IsActive = true,
                CreatedDate = newest.AddDays(-index),
            });
        }
    }
}
