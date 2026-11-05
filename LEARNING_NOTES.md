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

### Akılda kalsın (Faz 0)
- `ViewDataDictionary` büyük/küçük harf duyarsızdır, bu yüzden `ViewBag.subTitle` ile `ViewBag.subtitle` aynı değeri okur. İleride ViewBag yerine strongly-typed model kullanacağız; o zaman yazım hatalarını derleyici yakalar.

---

## Faz 1: Veri modelini tamamlama

### Ne yaptık?
- Yeni entity'ler: `Skill` (ad, seviye %, sıra, aktif) ve `SiteSetting` (tek satırlık ayarlar: site başlığı, CV dosya yolu).
- `Experience` ve `Education`'a `StartDate` / `EndDate` (`DateOnly`) ekledik. `Education`'daki string `Duration` alanını kaldırdık. `Project`'e `IsActive` ve `CreatedDate` ekledik. `About.OpenToWork`'ü `string` yerine `bool` yaptık.
- Bütün entity'lere Data Annotations yazdık: `Required`, `StringLength`, `Range`, `EmailAddress`, `Url`, `Display`.
- Kategori silme davranışını Fluent API ile `Cascade`'den `Restrict`'e çevirdik.
- Admin deneyim formlarına tarih alanlarını, `CreateExperience` POST'una da `ModelState.IsValid` kontrolü ve `[ValidateAntiForgeryToken]` ekledik. Liste artık başlangıç tarihine göre sıralanıyor.

### Neden?
- **Tarih neden string değil?** "2021 - 2023" gibi bir metin sıralanamaz ve filtrelenemez. Case "tarih sırasına göre listele" diyor, bunun için gerçek bir tarih tipi gerekir. `DateOnly` saat bilgisi taşımaz ve SQL Server'da `date` kolonuna denk gelir. `EndDate = null` "hâlâ devam ediyor" demektir.
- **Data Annotations iki iş yapar:**
  1. *Validasyon:* `ModelState.IsValid` bu kurallara bakar. `asp-validation-for` hata mesajını formda gösterir.
  2. *Şema:* `[Required]` kolonu `NOT NULL`, `[StringLength(100)]` kolonu `nvarchar(100)` yapar. Kural yoksa her şey `nvarchar(max) NULL` olur.
  `[Range]`, `[EmailAddress]` ve `[Url]` ise yalnızca validasyondur, DB'ye yansımaz.
- **`Category? Category` neden nullable?** `<Nullable>enable</Nullable>` açıkken MVC, non-nullable bir referans tipini otomatik olarak *zorunlu* sayar. Formdan sadece `CategoryId` gelir, `Category` nesnesi gelmez. Navigation property non-nullable olsaydı form her seferinde "Category alanı zorunlu" diye reddedilirdi.
- **Kategori seçimi neden `[Range(1, ...)]`?** `int` hiçbir zaman null olmaz. Dropdown'da seçim yapılmazsa değer 0 gelir. "0'dan büyük olmalı" kuralı aslında "bir kategori seç" demek.
- **Data Annotation mı, Fluent API mı?** Basit kurallar (zorunlu, uzunluk) entity üstünde annotation olarak yazılır. İlişki davranışı (silme kuralı, bileşik anahtar vb.) annotation ile ifade edilemez; onlar `OnModelCreating` içinde Fluent API ile yazılır.
- **Restrict neden?** `Cascade` ile bir kategori silinince ona bağlı bütün projeler de sessizce silinirdi. `Restrict` ile DB bunu reddeder; Faz 4'te admin'e "Bu kategoride X proje var" diye anlamlı bir mesaj göstereceğiz.

### Yaşadığımız hata ve dersi
- İlk `database update` şu hatayla düştü: *"String or binary data would be truncated… HeroSections.ImageUrl"*. Mevcut görsel URL'i 320 karakterdi, biz kolonu 300'e daraltmaya çalışmıştık.
- **Ders 1:** Kolon daraltırken önce mevcut veriye bak (`SELECT MAX(LEN(...))`). EF'in "may result in the loss of data" uyarısı ciddiye alınmalı.
- **Ders 2:** EF Core migration'ı SQL Server'da transaction içinde çalıştırır. Hata alınca hiçbir değişiklik kalmadı ve DB `mig3`'te kaldı. `migrations remove` ile migration'ı silip limitleri 500'e çıkardık, yeniden oluşturduk ve sorunsuz uyguladık.
- Uygulanmamış bir migration `migrations remove` ile güvenle silinebilir. Uygulanmış bir migration'ı geri almak içinse önce `database update <öncekiMigration>` gerekir.

---

## Faz 2: Tasarım dili ve cam ikon seti

### Ne yaptık?
- Tasarım dilini belirledik: koyu zemin ve mühendislik ızgarası, cam (glassmorphism) yüzeyler, tek vurgu rengi kırmızı `#E5322D`, IBM Plex Sans ve IBM Plex Mono. Gradyan, başlık üstü etiket ve emoji kullanmıyoruz.
- 11 ana ikonu Higgsfield ile ürettik: önce **GPT Image 2.5** ile statik cam görsel, sonra **MiniMax H3** ile 5 saniyelik döngüsel animasyon. Pilotta MiniMax ile Kling 4.0'ı aynı görselle karşılaştırıp MiniMax'i seçtik.
- `design/icons/icons.json`: tek bir stil şablonu ve her ikon için yalnızca konu, kırmızı detay ve hareket tanımı.
- `design/icons/generate.py`: `image → video → convert` aşamalarından oluşan, paralel çalışan, tekrar kullanılabilir bir üretim hattı.
- Web çıktısı `wwwroot/assets/icons/`: her ikon için alfa kanallı `.webm` ve şeffaf `.png` poster. 11 ikon toplam 2.6 MB.

### Neden?
- **Tek şablon, değişen parçalar:** Promptları tek tek elle yazsaydık ışık, açı ve malzeme ikondan ikona kayardı. Ortak kısım (cam, ışık, açı, arka plan) sabit kaldığı için set bir aile gibi görünüyor. Yeni bir ikon eklemek JSON'a 4 satır yazmak kadar kolay.
- **Döngü:** Videoların başlangıç ve bitiş karesi aynı görsel (`--start-image` ve `--end-image`). Bu yüzden tekrar ederken atlama olmuyor.
- **Neden düz renkli arka plan istedik?** Video formatlarının çoğu şeffaflık taşımaz. Bu yüzden modelden düz koyu bir arka plan istedik, sonra ffmpeg'in `colorkey` filtresiyle o rengi silip **VP9 + alfa** (`yuva420p`) WebM ürettik.
- **İlk deneme neden olmadı?** `mix-blend-mode: screen` ile koyu pikselleri gizlemeye çalıştık, ama cam kartın `backdrop-filter`'ı yeni bir katman grubu (isolation) oluşturuyor. Karıştırma bu grubun dışına çıkamadığı için ikonun etrafında koyu bir kare kaldı. Ders: CSS'te görsel bir hileye güvenmek yerine asset'in kendisini doğru üretmek daha sağlam.
- **Küçük ikonlar neden video değil?** 16 piksellik hareketli bir ikon okunmaz ve sayfayı gereksiz ağırlaştırır. Hareketli cam ikonlar section ve dashboard gibi büyük yerlerde; butonlar ve menüler ince çizgili, tutarlı bir ikon setiyle kalacak.
- **Erişilebilirlik ve performans:** `muted autoplay loop playsinline` tarayıcıların otomatik oynatmaya izin vermesi için şart. `prefers-reduced-motion` açıksa video yerine statik PNG gösterilecek (Faz 3).

### Yaşadığımız sorunlar
- 10 paralel istekten biri `403 Forbidden` aldı. Tek başına yeniden denenince geçti; muhtemelen eşzamanlı istek sınırına takılmıştı.
- Dashboard kadranının yüzü opak beyaz çıktı ve camsı değildi. Prompta "kadran yüzü de buzlu cam, beyaz değil" diye açıkça yazınca düzeldi. Ders: Model, belirtmediğin ayrıntıyı en olası haliyle doldurur.

### Açık konu (Faz 3)
- Safari, VP9 alfa WebM'i şeffaf göstermiyor. Safari'de video yerine şeffaf PNG poster gösteren küçük bir yedek (fallback) ekleyeceğiz.
