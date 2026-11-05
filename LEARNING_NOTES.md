# Öğrenme Notları

Her fazdan sonra "ne yaptık, neden yaptık" notu. Proje sonunda soru-cevap ve video script'i bu dosyadan çıkacak.

**Proje:** ASP.NET Core MVC (.NET 10) + EF Core 10 (Code First) + SQL Server LocalDB ile yönetilebilir portfolyo sitesi ve admin paneli.

---

## Faz 0: Temeli sağlamlaştırma

### Ne yaptık?
- `Program.cs` içinde iki kez yapılan `AddDbContext<PortfolioContext>()` kaydını teke indirdik.
- Category ↔ Project bire-çok ilişkisini (`mig3`) gözden geçirip commit'ledik.
- Git hijyenini kontrol ettik: `.vs/`, `bin/`, `obj/` ve `*.user` repoda takip edilmiyor.
- `dotnet ef migrations list` ile bütün migration'ların DB'ye uygulandığını doğruladık.

### Neden?
- **Dependency Injection (DI):** Controller'lar `PortfolioContext`'i kendileri `new`'lemez, constructor'dan ister. Nesneyi oluşturup veren DI container'dır. Bu sayede bağımlılıklar tek bir yerden (`Program.cs`) yönetilir.
- **Çift kayıt neden sorundu?** İlk kayıt (`AddDbContext<PortfolioContext>()`) hiçbir ayar içermiyordu, asıl ayar (connection string) ikinci kayıttaydı. Güncel EF Core sürümleri ikisini birleştirdiği için uygulama çalışıyordu, ama bu şansa bağlı bir durum. Kodu okuyan biri "hangisi geçerli?" diye düşünmek zorunda kalır, eski sürümlerde de farklı davranabilir. Kural: bir servisin tek ve açık bir kaydı olur.
- **DbContext ömrü (lifetime):** `AddDbContext` varsayılan olarak *Scoped* kayıt yapar, yani her HTTP isteğine bir DbContext. DbContext thread-safe olmadığı için uygulama genelinde paylaşılmamalı (Singleton olmamalı).
- **Migration nedir?** C# entity'lerindeki değişikliğin veritabanı şemasına çevrilmiş, versiyonlanmış hali. `Up()` değişikliği uygular, `Down()` geri alır. `ModelSnapshot` dosyası EF'in "modelin son hali" hafızasıdır; yeni migration oluştururken farkı buna bakarak hesaplar. Bu yüzden migration dosyaları da repoya commit'lenir.
- **Bire-çok ilişki:** `Project.CategoryId` foreign key'dir (FK). `Project.Category` ve `Category.Projects` ise navigation property'lerdir; C# tarafında ilişkili nesnelere bunlar üzerinden ulaşırız. EF, FK'yı isimlendirme kuralından (convention) kendisi tanır. `onDelete: Cascade` olduğu için kategori silinince ona bağlı projeler de silinir (Faz 4'te bunu kısıtlayacağız).

### Akılda kalsın
- `ViewDataDictionary` büyük/küçük harf duyarsızdır, bu yüzden `ViewBag.subTitle` ile `ViewBag.subtitle` aynı değeri okur. İleride ViewBag yerine strongly-typed model kullanacağız; o zaman yazım hatalarını derleyici yakalar.
