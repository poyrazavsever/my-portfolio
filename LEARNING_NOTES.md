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

---

## Faz 3: Tasarım sistemi ve admin kabuğu

### Ne yaptık?
- `wwwroot/css/design-system.css`: Bütün renk, cam, tipografi ve ölçü kararları `:root` token'larında. Buton, form, tablo, rozet, cetvel göstergesi, boş durum ve modal bileşenleri de burada. Public site ile admin ortak kullanacak.
- `wwwroot/css/admin.css`: Admin kabuğu (sidebar, içerik alanı, mobil üst bar ve offcanvas).
- `GlassIconTagHelper` ve `glass-icon.js`: `<glass-icon name="projects" size="64" />` yazınca hareketli cam ikon çıkıyor.
- Admin layout'u `Areas/Admin/Views/Shared/_AdminLayout.cshtml`'e taşıdık ve area'ya `_ViewStart.cshtml` ekledik. Eski `AdminLayoutController`, 630 satırlık stil component'i ve sahte veriyle çalışan 720 satırlık JS silindi.
- Sidebar View Component'i menüyü bir model listesinden üretiyor ve okunmamış mesaj sayısını DB'den çekiyor. Mobilde aynı component bir offcanvas içinde tekrar kullanılıyor.
- `/Admin` adresi için `DashboardController` eklendi (içeriği Faz 9'da gelecek).
- Deneyim ve Mesaj sayfaları yeni dile taşındı. Ekle ve Düzenle formları ortak partial'ları (`_ExperienceForm`, `_MessageForm`) kullanıyor. İstemci taraflı validasyon (jQuery Validate Unobtrusive) açık.

### Neden?
- **Design token'lar:** Kırmızıyı değiştirmek istersek tek satır değişecek. Renk kodları dosyalara dağılmış olsaydı her bileşeni tek tek bulmamız gerekirdi.
- **Bootstrap'ı neden atmadık?** Modal, offcanvas ve grid zaten çalışıyor; case de Bootstrap Modal istiyor. Bootstrap 5.3 renklerini `--bs-*` CSS değişkenlerinden okuyor. `[data-bs-theme="dark"]` altında bu değişkenleri kendi token'larımıza yönlendirince bütün Bootstrap bileşenleri kendiliğinden bizim dilimizle görünüyor.
- **`_ViewStart`:** Bir klasördeki bütün view'lar için varsayılan layout'u belirler. Önceden 6 view `Layout = "~/Areas/.../Index.cshtml"` diye aynı satırı tekrar ediyordu.
- **`@RenderSection("Actions")`:** Sayfa başlığı tek yerde (layout'ta) çiziliyor. Her sayfa sadece kendi butonlarını bu bölüme koyuyor. `ViewData["Title"]` ve `ViewData["Description"]` hem `<title>`'ı hem de sayfa başlığını besliyor.
- **Partial View mı, View Component mi?**
  - *Partial* sadece kendisine verilen modeli çizer (form alanları gibi).
  - *View Component* kendi verisini kendisi çeker (sidebar'daki okunmamış mesaj sayısı gibi). Sayfayı açan controller'ın bundan haberi olmasına gerek kalmaz.
- **Tag Helper:** HTML gibi görünen ama sunucuda C# ile işlenen etiket. `asp-for` ve `asp-action` da birer Tag Helper. `<glass-icon>` için kendimizinkini yazıp `_ViewImports`'ta `@addTagHelper *, Portfolio` ile tanıttık.
- **Kademeli iyileştirme (progressive enhancement):** Sunucu her zaman şeffaf PNG basıyor, bu her yerde çalışır. JS yalnızca destekleyen tarayıcılarda bunu videoya yükseltiyor. Safari'de veya "azaltılmış hareket" açıkken PNG kalıyor. Ekran dışına çıkan video `IntersectionObserver` ile duraklatılıyor.
- **Silme neden form ve POST?** GET isteği "bir şey oku" demektir. Tarayıcılar ve arama botları GET linklerini önceden açabilir. Veri değiştiren her işlem POST olmalı ve `[ValidateAntiForgeryToken]` ile korunmalı. `<form method="post">` Tag Helper'ı token'ı forma kendisi ekliyor.

### Tarayıcıda yakaladığımız hatalar
- **Aktif menü çizgisi görünmüyordu:** Kırmızı çizgi linkin 12px soluna konumlanmıştı ama menü listesinin `overflow-y: auto` olması onu kesiyordu. Çizgiyi linkin içine aldık.
- **Mobilde sayfa yana taşıyordu:** Tablo başlığındaki ekran okuyucu metni (`.visually-hidden`) `position: absolute`. Kaydırma kabında `position` olmadığı için kabın dışına kaçıp sayfayı 689px'e genişletiyordu. Kaba `position: relative` verince düzeldi.
  - Ders: `overflow` kırpması yalnızca kabı "konumlandırılmış ata" kabul eden absolute öğeler için çalışır.
- **Modal ekran görüntüsünde görünmedi** ama DOM'da açıktı. Animasyon bitmeden çekilen bir ekran görüntüsüydü. Ders: Görsel testte önce DOM durumunu doğrula.

---

## Faz 4: Admin CRUD modülleri

### Ne yaptık?
- **Tek kayıtlı sayfalar (upsert):** Profil (Hero ve İletişim tek sayfada, iki ayrı form), Hakkımda, Ayarlar.
- **Liste ve tam CRUD:** Eğitim, Yetenekler, Kategoriler, Projeler. Deneyim controller'ı da aynı standarda çekildi (async, TempData, tarih kontrolü).
- **`IImageStorage` servisi:** Uzantı beyaz listesi, 2 MB sınırı, dosya imzası (magic bytes) kontrolü, `Guid` ile benzersiz ad ve `wwwroot/uploads/<klasör>/` altında saklama. Eski görsel değişince veya kayıt silinince dosya da siliniyor.
- **Geçici bildirim:** `TempData["Success"]` / `TempData["Error"]` ve `_Alerts` partial'ı (Faz 5'te SweetAlert2'ye yükselecek).
- `Program.cs`'e `AddScoped<IImageStorage, ImageStorage>()` ve `UseStaticFiles()` eklendi.

### Neden?
- **Upsert:** Hero, Hakkımda ve Ayarlar tablolarında tek satır olur. `FirstOrDefault()` ile kayıt yoksa `Add`, varsa alanları kopyalayıp `SaveChanges`. Böylece "Ekle" butonuna gerek kalmıyor.
- **ViewModel ve `[Bind(Prefix = "Hero")]`:** Bir view tek model alır; Profil iki tabloyu birlikte gösterdiği için `ProfileViewModel { Hero, Contact }` yazdık. `asp-for="Hero.Title"` alanı `name="Hero.Title"` olarak üretir. POST'ta `[Bind(Prefix = "Hero")] HeroSection hero` model binder'a "bu parametreyi `Hero.` ile başlayan alanlardan doldur" der. Hata mesajları da doğru alana (`Contact.Linkedin`) düşer.
- **Neden arayüz (`IImageStorage`)?** Controller somut sınıfa değil sözleşmeye bağlı. Yarın dosyaları bulut depolamaya taşımak istersek sadece `Program.cs`'teki kayıt değişir. *Scoped* = istek başına bir örnek.
- **Dosya yükleme güvenliği:**
  - Kullanıcının dosya adı asla kullanılmıyor: `../../web.config` gibi isimlerle başka yerlere yazma (path traversal) ve çakışma riski var.
  - Uzantı tek başına güvenilmez; dosyanın ilk baytlarına bakıyoruz (JPEG `FF D8 FF`, PNG `89 50 4E 47`, WEBP `RIFF....WEBP`).
  - Silme işlemi yalnızca `/uploads/` altındaki dosyalara izin veriyor.
- **`MapStaticAssets` ile `UseStaticFiles` farkı:** .NET 9+ `MapStaticAssets` build anında wwwroot'taki dosyaların listesini (manifest) çıkarır ve sadece onları servis eder. Çalışma zamanında yüklenen dosyalar o listede yok, `UseStaticFiles` olmadan 404 alırlardı.
- **`SelectList` ve `asp-items`:** `new SelectList(kategoriler, "CategoryId", "CategoryName", seçiliId)` dropdown seçeneklerini üretir. "Kategori seçin" seçeneğinin değeri `0`; `[Range(1, ...)]` kuralı bunu "bir kategori seç" hatasına çevirir.
- **`Include`:** EF ilişkili veriyi kendiliğinden yüklemez. `project.Category` dolu gelsin diye `Include(p => p.Category)` ile SQL'de JOIN yaptırıyoruz.
- **Projeksiyon (`Select`):** Kategori listesinde proje *sayısı* gerekiyor, projelerin kendisi değil. `Select(c => new { ..., ProjectCount = c.Projects.Count })` EF tarafından tek bir SQL'e (alt sorguda `COUNT`) çevrilir. `Include` ile bütün projeleri çekmekten çok daha hafif.
- **`IQueryable` ile koşullu filtre:** `query = query.Where(...)` sorguyu çalıştırmaz, sadece tarif eder. SQL ancak `ToListAsync()` çağrıldığında oluşur; böylece filtreyi koşullu ekleyebiliyoruz.
- **Restrict ve iki katmanlı koruma:** Kategori silinmeden önce `CountAsync` ile proje sayısına bakıp anlaşılır bir mesaj veriyoruz. Arada başka biri proje eklerse DB'deki Restrict kuralı silmeyi reddeder ve biz `DbUpdateException`'ı yakalarız.
- **İki alanı karşılaştıran kural:** Data Annotations tek bir alana bakar. "Bitiş, başlangıçtan önce olamaz" kuralını controller'da `ModelState.AddModelError` ile ekledik.
- **PRG (Post-Redirect-Get):** Başarılı POST'tan sonra `RedirectToAction`. Kullanıcı F5'e basınca form tekrar gönderilmez. TempData mesajı bu yönlendirmeyi tam bir kez atlatacak kadar yaşar.

### Nasıl test ettik?
- Tarayıcıda: kategori ekleme, aynı adla tekrar ekleme (benzersizlik hatası), içinde proje olan kategoriyi silme (Restrict mesajı), bitişi başlangıçtan önce olan eğitim, 150 seviyeli yetenek (`Range` hatası), geçersiz LinkedIn URL'si (Prefix'li hata) ve upsert kayıtları.
- Dosya yüklemeyi `curl` ile gerçek multipart POST olarak test ettik: geçerli PNG kaydedildi; içi metin olan sahte `.jpg` ve 2.5 MB'lık dosya reddedildi ve diske hiçbir şey yazılmadı. Proje silinince görsel de silindi. Token'sız silme isteği `400` ile reddedildi.

---

## Faz 5: Bildirim altyapısı (SweetAlert2 ve TempData)

### Ne yaptık?
- `Infrastructure/NotificationExtensions.cs`: `TempData.Success("...")`, `TempData.Error("...")`, `TakeSuccess()`, `TakeError()`. Controller'lardaki 22 adet `TempData["Success"]` bu metotlara çevrildi.
- SweetAlert2 11.14.5 (sabit sürüm) eklendi ve tasarım diline temalandı: cam yüzey, bizim `btn` sınıflarımız, renkli ikonlar yerine tipografi.
- `wwwroot/js/admin-ui.js`:
  1. `data-confirm` niteliği taşıyan her form gönderilmeden önce onay penceresi açılıyor. Pencerede silinecek kaydın adı yazıyor, odak "Vazgeç"te.
  2. Sunucunun bastığı `data-flash` uyarı kutuları sağ altta toast'a çevriliyor. Hata mesajları daha uzun ekranda kalıyor.
- 6 liste sayfasındaki `onsubmit="return confirm(...)"` kaldırıldı, yerine `data-confirm` ve `data-confirm-title` geldi.

### Neden?
- **Magic string yerine extension metot:** `TempData["Sucess"]` gibi bir yazım hatası derlenir ama mesaj sessizce kaybolur. `TempData.Success(...)` yanlış yazılırsa derleme hatası olur. Anahtar tek yerde (`Notify.Success`) tutulduğu için ileride değiştirmek de kolay.
- **TempData nasıl çalışır?** Değer çerezde (varsayılan provider) bir sonraki isteğe taşınır. Indexer ile okunduğu an "silinmek üzere" işaretlenir; `TempData.Peek()` ise silmeden okur. Bizim akışımız PRG: POST'ta yaz, redirect et, GET'te bir kez göster.
- **Neden `data-*` nitelikleri?** HTML yalnızca niyeti ("bu form onay ister") söylüyor, davranışı tek bir JS dosyası ekliyor. Yeni bir silme formuna onay eklemek için tek satır nitelik yeterli. Inline `onsubmit` her sayfaya JS kopyalamak demekti.
- **Event delegation:** Her forma ayrı dinleyici bağlamak yerine `document`'a tek bir `submit` dinleyicisi koyduk. Olay form'dan yukarı kabarıyor (bubbling); sayfaya sonradan eklenen formlar da otomatik kapsanıyor.
- **Kademeli iyileştirme:** SweetAlert2 CDN'den yüklenemezse onay tarayıcının `confirm()` penceresine düşüyor. JS hiç çalışmazsa sunucunun bastığı uyarı kutusu sayfada görünür kalıyor. Yani hiçbir durumda bilgi kaybolmuyor.
- **XSS güvenliği:** Onay metni ve toast mesajı kullanıcı verisi içeriyor (ör. kategori adı). SweetAlert2'ye `html` yerine `text` olarak veriyoruz ve toast'ta `textContent` kullanıyoruz. Adı `<script>` olan bir kategori bile kod olarak çalışmaz.
- **Güvenli varsayılan:** Onay penceresinde odak "Vazgeç"te (`focusCancel`). Yanlışlıkla Enter'a basmak silme yapmaz. Onaydan sonra buton devre dışı kalıyor ki çift tıklama iki istek göndermesin.

### Tarayıcıda ne denedik?
- Yetenek ekle: başarı toast'ı çıktı, düz uyarı kutusu kaldırıldı.
- Silmede "Vazgeç": kayıt duruyor. "Sil": kayıt silindi ve toast çıktı.
- İçinde proje olan kategoriyi silme: kırmızı hata toast'ı çıktı.
- SweetAlert2'nin kendi `.swal2-html-container` kuralı metni ortalıyordu. Seçicimizi daha spesifik yaparak (`.swal2-popup .pa-swal-text`) düzelttik. Ders: Bir kütüphaneyi temalarken CSS spesifikliği önemli.
- *Denenmedi:* CDN'in yüklenemediği durumdaki `confirm()` yedeği; kodu okunarak doğrulandı.

---

## Faz 6: Public UI'ın yeniden tasarımı, DB'ye bağlanması ve iletişim formu

### Ne yaptık?
- **Temizlik:** 1000 satırlık inline stil component'i, sayfa içindeki 150 satırlık inline script, şablondan kalan Home sayfaları ve boş `Views/Index.cshtml` silindi. Site kökü `/` artık portfolyoya gidiyor (`{controller=Default}`); `HomeController` yalnızca hata sayfası için kaldı.
- **Layout:** `_Layout.cshtml` artık portfolyonun kabuğu: head, navbar ve footer View Component'leri, `design-system.css` + `site.css`, `glass-icon.js` + `site.js`. Hata sayfası da aynı tasarımı kullanıyor.
- **Bölümler:** Hero, Hakkımda, Deneyim, Eğitim, Yetenekler, Projeler, Çalışma Yaklaşımı (statik partial), Sertifikalar ve İletişim. Her biri kendi View Component'inde kendi verisini çekiyor. `Default/Index.cshtml` sadece sıralamayı belirliyor.
- **Sertifika modülü:** `Certificate` entity'si, `mig5_Certificates` migration'ı ve admin CRUD'u eklendi. Çalışma Yaklaşımı bilinçli olarak statik bırakıldı.
- **Projeler:** Kategori filtresi (yalnızca aktif projesi olan kategoriler, sayılarıyla) ve Türkçe harf duyarlı arama. Sonuç yoksa "Filtreleri temizle" butonlu boş durum mesajı.
- **İletişim formu:** Doğrulama, DB'ye kayıt, `[Bind]` ile overposting koruması, honeypot ile bot koruması, PRG ile teşekkür kartı.
- **Tasarım kuralları:** Başlık üstü etiket yok. Her bölüm başlığının solunda hareketli cam ikon var (`_SectionHead` partial'ı). Gradyan ve emoji yok. Deneyim ve eğitim, ölçü çizgisi ve çentikli bir zaman çizelgesiyle gösteriliyor; yetenekler cetvel göstergesiyle.

### Neden?
- **Her bölüm bir View Component:** Hero; Hero, Hakkımda, İletişim ve Ayarlar tablolarından besleniyor. Bu sorguları `DefaultController.Index`'e yığsaydık controller şişerdi. View Component'ler kendi verisini çekiyor, controller "sayfa" seviyesinde kalıyor. Navbar ve footer'ın ortak sorgusu ise `SiteQueries.LoadChromeAsync`'te tek yerde.
- **ViewBag'den strongly-typed modele:** Eski hero `ViewBag.title`, `ViewBag.subTitle` kullanıyordu. `HeroViewModel` ile alan adını yanlış yazmak artık derleme hatası.
- **Boş veride bölüm hiç çizilmez:** `return values.Count == 0 ? Content(string.Empty) : View(values);`. Boş bir "Sertifikalar" başlığı göstermek yerine bölüm sayfadan tamamen çıkıyor.
- **Overposting ve `[Bind]`:** `Message` entity'sinde `IsRead`, `SendDate` gibi alanlar da var. Model binder formda ne gelirse bağlar. `[Bind("NameSurname", "Email", "Subject", "MessageDetail", Prefix = "Form")]` yalnızca bu dört alana izin veriyor. curl ile `Form.IsRead=true` gönderdik; kayıt yine okunmamış olarak oluştu.
- **Honeypot:** Görünmeyen bir "website" alanı. İnsan doldurmaz, bot doldurur. Doluysa kaydetmiyoruz ama başarılı gibi yanıt veriyoruz ki bot yakalandığını anlamasın.
- **Hata olursa sayfayı yeniden çizmek:** Form bir View Component içinde, sayfa ise `Default/Index`. Geçersiz POST'ta `View("Index")` döndürüp formu `ViewData["ContactForm"]` ile component'e veriyoruz. View Component sayfanın `ModelState`'ini paylaştığı için `asp-validation-for` hataları kendiliğinden görünüyor.
- **`asp-fragment="contact"`:** Form `/Default/SendMessage#contact` adresine gidiyor. Hata dönerse tarayıcı cevap sayfasında doğrudan iletişim bölümüne açılıyor; JS gerekmiyor.
- **`overflow-x: clip` ve `hidden` farkı:** Hero'daki teknik çizim bilerek sağa taşıyor. `clip` taşan kısmı keser ama `hidden` gibi bir kaydırma kabı oluşturmaz (sticky/fixed davranışını bozmaz).
- **Türkçe arama:** `"İLETİŞİM".toLowerCase()` "i̇letişim" gibi bozuk sonuç verebilir. `toLocaleLowerCase("tr-TR")` doğru çalışıyor.
- **Scrollspy:** `IntersectionObserver` ekranın üst-orta bandına giren bölümü bulup navbar'da işaretliyor. Scroll olayında sürekli hesap yapmaktan çok daha verimli.

### Tarayıcıda yakaladığımız sorunlar
- **Yatay taşma (1252/1132 px):** Teknik çizim sayfayı genişletiyordu → `overflow-x: clip`.
- **Hakkımda metni boşken sol sütun boş kalıyordu** → metin yoksa `is-compact` ile tek sütun.
- **`.cshtml` değişikliği yansımadı:** Razor view'ları build sırasında derleniyor; CSS ve JS anında yansır ama view değişikliği için yeniden build gerekir.
- **`#contact`'a atlama:** Yumuşak kaydırmayı sadece sayfa yüklendikten sonra açtık (`html.is-ready`), böylece ilk açılışta atlama anında oluyor. Fragment'e atlama klavye odağını sıfırladığı için ilk hatalı alana `load` olayından sonra odaklanıyoruz.
  - *Not:* Arka planda çalışan otomasyon sekmesi ilk yüklemede fragment'e hiç kaydırmıyor (normal `/#projects` linkinde bile). Kodu `hash` değişimiyle doğruladık; gerçek tarayıcıda gözle kontrol edilmeli.

### Nasıl test ettik?
- Masaüstü ve mobil (375 px): taşma yok, mobil menü açılıyor ve link tıklanınca kapanıyor, zaman çizelgesi tek sütuna iniyor.
- Proje arama ("xyz" → boş durum, "CMS" ve "WEB" → sonuç), filtre temizleme.
- İletişim: boş/geçersiz form → 4 hata ve yazılanlar korunuyor; geçerli form → teşekkür kartı ve DB'de okunmamış kayıt; curl ile overposting ve honeypot.

---

## Ara adım: Portre, logo ve favicon

### Ne yaptık?
- **Hero portresi:** Gönderilen 1254×1254 fotoğrafı hero çerçevesine uysun diye 4:5 oranında kırpıp 880×1100 WebP yaptık (`/assets/profile/portrait.webp`, 69 KB). DB'deki `HeroSections.ImageUrl` artık bu dosyayı gösteriyor.
- **Logo:** Daha önce link olarak verilen görselin 1254 px'lik orijinalini yüze odaklı kırptık. 96 px (1x) ve 192 px (2x) WebP olarak kaydettik. `_BrandLogo` partial'ı public navbar/footer'da ve admin sidebar/üst bar'da kullanılıyor; admin'deki "PA" kutusu da artık bu fotoğraf.
- **Favicon seti:** Pillow ile 16/32/48 boyutlarını içeren gerçek bir `favicon.ico`, 16/32 PNG, 180 px `apple-touch-icon`, 192/512 PWA ikonları ve `site.webmanifest`. Hepsi `_Favicons` partial'ıyla iki layout'a da eklendi.

### Neden?
- Link verilen görsel aslında 128×128 boyutuna küçültülmüş bir Next.js görseliydi. 96 px'lik logo retina ekranlarda 192 px kaynak ister; bu yüzden URL'in içindeki orijinal adrese gidip tam çözünürlüklü sürümü kullandık.
- Logo ekranda 34 px görünüyor; dosya 96/192 px. `srcset` ile tarayıcı ekran yoğunluğuna göre uygun dosyayı seçiyor.
- Tasarım varlıkları `wwwroot/assets/` altında ve repoya giriyor. Admin'den yüklenenler `wwwroot/uploads/` altında ve git'e girmiyor. `ImageStorage.Delete` yalnızca `/uploads/` altını sildiği için portreyi admin'den değiştirmek bu dosyayı silmez.

---

## Faz 7: Gelen kutusu

### Ne yaptık?
- **Liste:** Arama (gönderen adı, e-posta, konu), durum filtresi (tümü/okunmamış/okunmuş) ve sıralama (en yeni/en eski). Filtreler bir `GET` formuyla URL'de taşınıyor (`?q=&status=&sort=`).
- **Okunmamış sayısı:** Sayfadaki özet şeridinde ve sidebar rozetinde gösteriliyor. Okunmamış satırlar kalın ve kırmızı noktalı.
- **Detay:** Bootstrap Modal. Modal açılınca mesaj `fetch` ile arka planda okundu işaretleniyor; sayaçlar sayfa yenilenmeden güncelleniyor.
- **Okundu/okunmadı işaretleme:** Listeden ve modaldan iki yönlü (`ToggleRead`). İşlemden sonra aynı filtreli görünüme dönülüyor.
- **`?open=12`:** Bu adresle gelindiğinde ilgili mesajın modalı kendiliğinden açılıyor; Dashboard'daki "son mesajlar" listesi bunu kullanacak.
- **Orijinal içerik düzenlenemez:** `UpdateMessage` ve `CreateMessage` action'ları ve view'ları kaldırıldı. Eski düzenleme adresi artık 404.
- `DeleteMessage` artık yalnızca POST ve antiforgery korumalı (önceden GET ile de silinebiliyordu).

### Neden?
- **Filtreler neden `GET`?** Filtreleme veri değiştirmez, sadece okur. `GET` ile adres paylaşılabilir, yer imine eklenebilir, F5 ve geri tuşu doğal çalışır. Veri değiştiren işlemler ise (`ToggleRead`, `DeleteMessage`) POST.
- **`IQueryable` ile koşullu sorgu:** `Where` ve `OrderBy` sırayla ekleniyor, SQL ancak `ToListAsync()` anında oluşuyor. `Contains` SQL'de `LIKE '%...%'` oluyor; SQL Server'ın varsayılan collation'ı büyük/küçük harfe duyarsız olduğu için "EXAMPLE.COM" araması da sonuç buluyor.
- **AJAX ve antiforgery:** `[ValidateAntiForgeryToken]` token'ı form alanında *veya* `RequestVerificationToken` header'ında arar. Sayfaya `@Html.AntiForgeryToken()` ile gizli bir token basıp `fetch` ile header'da gönderdik. Token'sız istek `400` aldı.
- **Event delegation, yine:** Her modala ayrı dinleyici yerine `document`'ta tek bir `show.bs.modal` dinleyicisi var. Bootstrap her modal açılışında bu olayı yayınlıyor.
- **Open redirect:** `returnUrl` kullanıcıdan gelen bir değer. Kontrolsüz `Redirect(returnUrl)` saldırganın `?returnUrl=https://kotu-site.com` ile kullanıcıyı sahte bir giriş sayfasına yollamasına izin verir. `Url.IsLocalUrl` yalnızca site içi adreslere izin veriyor: `https://...`, `//...` (protokolsüz) ve `/\...` denemeleri listeye yönlendirildi.

### Test sırasında öğrendiklerimiz
- **Git Bash yol dönüşümü:** `//kotu-site.example` argümanı MSYS tarafından dosya yolu sanılıp `/kotu-site.example`'a çevrildi; test ilk bakışta açık varmış gibi gösterdi. `MSYS_NO_PATHCONV=1` ile tekrarlayınca korumanın çalıştığı görüldü. Ders: Test aracının girdini değiştirip değiştirmediğini doğrula.
- Aynı nedenle (Windows kodlaması) curl ile gönderilen Türkçe karakterler bozuk kaydedilmişti; tarayıcıdan gönderilenler sorunsuz.

---

## Faz 8: CV indir

### Ne yaptık?
- **`ICvStorage` / `CvStorage`:** CV, `wwwroot` dışında `App_Data/cv/<guid>.pdf` olarak saklanıyor. DB'de (`SiteSetting.CvFilePath`) yalnızca göreli yol tutuluyor. Klasör `.gitignore`'da.
- **Doğrulama (üç katman):** `.pdf` uzantısı, `application/pdf` içerik türü ve dosyanın ilk baytlarında `%PDF-` imzası. En fazla 5 MB.
- **Admin → Ayarlar:** Yükle/değiştir, yeni sekmede önizle, kaldır (SweetAlert onaylı). İndirilecek dosya adı ayarlanabiliyor; boş bırakılırsa ad soyaddan `Poyraz-Avsever-CV.pdf` üretiliyor. Yeni CV yüklenince eski dosya diskten siliniyor.
- **Public:** `GET /Default/DownloadCv` → `PhysicalFile(yol, "application/pdf", "Poyraz-Avsever-CV.pdf")`. Navbar ve hero'daki "CV indir" butonları yalnızca DB'de kayıt **ve** diskte dosya varsa görünüyor.
- **`[RequestSizeLimit(6 MB)]`** ile çok büyük istekler action'a ulaşmadan kesiliyor. Tarayıcıda da `data-max-bytes` ile seçim anında boyut kontrolü yapılıyor (görsel yükleme alanlarına da eklendi).

### Neden?
- **Neden `wwwroot` dışında?** `wwwroot` içindeki her dosya, adresini bilen herkese statik dosya olarak açıktır; erişimi, adı ve içerik türünü kontrol edemeyiz. `App_Data`'daki dosyaya ise yalnızca bizim action'ımız üzerinden ulaşılabiliyor. Test: `/App_Data/cv/...` adresi `404` verdi. İleride "CV'yi kaç kişi indirdi?" sayacı ya da "yalnızca giriş yapanlar indirebilir" kuralı eklemek tek bir action'a kod yazmak kadar kolay.
- **`ContentRootPath` ile `WebRootPath` farkı:** İlki projenin kök klasörü, ikincisi onun altındaki `wwwroot`. Görseller (herkese açık olmalı) `WebRootPath` altında, CV `ContentRootPath/App_Data` altında.
- **`PhysicalFile` ve `Content-Disposition`:** Üçüncü parametre (`fileDownloadName`) verildiğinde ASP.NET `Content-Disposition: attachment; filename=...` header'ı ekler. Tarayıcı dosyayı açmak yerine indirir ve diskteki Guid'li ad yerine bizim verdiğimiz anlamlı adı kullanır. Admin önizlemesinde bu parametreyi vermedik; PDF tarayıcıda açılıyor.
- **Neden üç ayrı kontrol?** Uzantı ve içerik türü istemciden gelir, kolayca sahtelenebilir. Asıl güvence dosyanın içeriği (`%PDF-`). İlk ikisi yine de faydalı: Kullanıcıya erken ve anlaşılır bir hata mesajı veriyorlar.
- **Sıralama önemli:** Yeni dosya önce diske yazılıyor, sonra DB güncelleniyor, en son eski dosya siliniyor. Arada bir hata olursa en kötü ihtimalle diskte yetim bir dosya kalır; site hiçbir zaman olmayan bir dosyayı göstermez.
- **Buton görünürlüğü neden dosyanın varlığına bağlı?** Repo yeni bir makinede kurulduğunda DB'de CV kaydı olup `App_Data` boş olabilir. O durumda tıklanınca 404 veren ölü bir buton göstermek yerine butonu hiç göstermiyoruz.
- **Path traversal koruması:** `GetFullPath` DB'den gelen yolu bile `App_Data` kökünün dışına çıkıyor mu diye kontrol ediyor (`../../appsettings.json` gibi bir değer işe yaramaz).

### Nasıl test ettik?
- Python ile geçerli bir PDF ve dört geçersiz dosya ürettik; `curl` ile yükledik:

| Deneme | Sonuç |
|---|---|
| İçi metin olan `.pdf` | ❌ İmza kontrolü |
| Geçerli PDF, tür `text/plain` | ❌ İçerik türü kontrolü |
| 5.5 MB | ❌ Servisin 5 MB sınırı |
| 7 MB | ❌ `RequestSizeLimit`, `400` (tarayıcıda artık JS ile önceden engelleniyor) |
| Geçerli PDF, sonra ikinci bir PDF | ✅ Butonlar çıktı, eski dosya silindi |
| İndirme | ✅ `attachment; filename=Poyraz-Avsever-CV.pdf`, içerik doğru sürüm |
| CV'yi kaldırma | ✅ Butonlar kayboldu, indirme `404` |

---

## Ara adım: Gerçek içerik ve seed

### Ne yaptık?
- poyrazavsever.com'dan (Hakkımda, Projeler, İletişim sayfaları) temel bilgileri topladık: rol tanımı, Hakkımda metni, 2 eğitim, 4 deneyim, 20 sertifika, 3 klasördeki 14 proje, e-posta, GitHub ve LinkedIn. Sosyal bağlantılar sitede kısa link olarak verildiği için gerçek adreslerini (`github.com/poyrazavsever`, `linkedin.com/in/poyrazavsever`) çözüp onları kullandık.
- `Data/PortfolioSeeder.cs`: Bu içeriği kodda tutan seed sınıfı. Komut: `dotnet run -- --seed`.
- Proje bağlantı alanı artık "Proje bağlantısı". Kart, GitHub adresiyse "Kaynak kod", değilse "Projeyi incele" yazıyor (migration gerektirmeyen bir görünüm değişikliği).

### Neden seed sınıfı?
- Veriyi admin panelinden elle girseydik sadece bu bilgisayardaki DB'de kalırdı. Seed sınıfı commit'lendiği için repoyu klonlayan biri `dotnet ef database update` + `dotnet run -- --seed` ile aynı içeriği görür. Case'in "örnek verilerle çalışır proje" teslim şartı da böylece karşılanıyor.
- **Komut satırı argümanı:** `args.Contains("--seed")` ise web sunucusu hiç başlamıyor; seed çalışıp program çıkıyor. `CreateScope()` gerekiyor çünkü `DbContext` *scoped* bir servis; HTTP isteği olmadan kendi kapsamımızı açmamız lazım.
- **`ExecuteDeleteAsync`:** Kayıtları belleğe çekmeden doğrudan `DELETE FROM ...` çalıştırıyor. Sıra önemli: Project → Category ilişkisi Restrict olduğu için önce projeler silinmeli.
- **Transaction:** Silme ve ekleme tek bir transaction'da. Yarıda hata olursa DB eski haline döner; yarı silinmiş, yarı dolu bir DB kalmaz.
- **Idempotent:** Komut kaç kez çalıştırılırsa çalıştırılsın sonuç aynı. Mesajlara ve yüklenen CV'ye dokunmuyor.

### Tahmin edilen veriler (siteden alınamayanlar)
- **Konum:** "Ankara, Türkiye". Sitede açıkça yazmıyor; OSTİM Teknik Üniversitesi'nden çıkarıldı.
- **Yetenek seviyeleri:** Teknoloji listesi sitede var ama seviye yok. Verilen yüzdeler yer tutucu; admin → Yetenekler'den güncellenmeli.
- **Ay bilgisi:** Eğitim tarihlerinde sitede yalnızca yıl var. Başlangıç Eylül, bitiş Haziran kabul edildi (sitede zaten yalnızca yıl gösteriliyor).

---

## Faz 9: Dashboard

### Ne yaptık?
- **`DashboardViewModel`:** Dashboard'un ihtiyaç duyduğu her şey tek bir modelde: 7 sayı, son 5 mesaj, son 3 proje, kategori özeti, CV durumu.
- **İstatistik kartları:** Toplam proje, aktif proje, yetenek, deneyim, eğitim, toplam mesaj, okunmamış mesaj. Her kart ilgili sayfaya gidiyor; okunmamış mesaj varsa kart kırmızı kenarlı ("yanıt bekliyor").
- **Son 5 mesaj:** Gönderen, konu, tarih, okundu durumu. Tıklanınca `MessageList?open=id` ile gelen kutusunda o mesajın modalı açılıyor.
- **Son 3 proje:** Ad, kategori, aktif/pasif. Tıklanınca düzenleme sayfası.
- **Kategori özeti:** Kategori başına proje sayısı, yatay çubuk grafik olarak. Üzerine gelince aktif sayısını da gösteren bir ipucu çıkıyor; altında "Tablo olarak göster" seçeneği var.
- **Hızlı işlemler:** Yeni proje, okunmamış mesajlar, portfolyoyu aç, CV yükle/güncelle.

### Neden?
- **ViewModel neden tek?** View'da sorgu yazmak (ör. `@Model.Count(...)`) iş mantığını görünüme taşır, test etmeyi ve okumayı zorlaştırır. Controller bütün sayıları hesaplayıp tek bir modelde veriyor; view yalnızca çiziyor. Case de bunu açıkça istiyor.
- **Sayılar neden ayrı `CountAsync`?** Her biri SQL'de tek bir `SELECT COUNT(*)`; satırların kendisi belleğe çekilmiyor. Bütün mesajları `ToList()` ile çekip bellekte saymak, mesaj sayısı büyüdükçe yavaşlardı.
- **`OrderByDescending` + `Take(5)`:** SQL'de `ORDER BY SendDate DESC` + `TOP(5)`. Sadece 5 satır geliyor.
- **`GroupBy`:** `Projects.GroupBy(p => p.Category.CategoryName).Select(g => new { g.Key, Count = g.Count(), Active = g.Count(p => p.IsActive) })` EF tarafından tek bir `GROUP BY` sorgusuna çevriliyor. Gruplama DB'de yapılıyor. Not: Hiç projesi olmayan kategori bu listede çıkmaz, çünkü gruplar projelerden oluşuyor. Sıfırları da göstermek istersek sorguyu `Categories` tablosundan başlatıp projeksiyon yapmak gerekir (Kategoriler sayfasında öyle yaptık).
- **Grafik kararları:**
  - Tek seri, tek renk (kırmızı); lejant gereksiz, başlık neyin gösterildiğini söylüyor.
  - Değerler çubuğun yanında yazılı, yani renk tek başına bilgi taşımıyor.
  - İnce çubuk, sadece uçta 4px yuvarlama; ızgara/eksen yok.
  - Ekran okuyucu ve kopyalama için aynı veri bir tablo olarak da var.
  - Grafik kütüphanesi (Chart.js vb.) eklemedik: 3 çubuk için 40 KB'lık bir kütüphane gereksiz; CSS yeterli.

### Tarayıcıda yakaladığımız sorun
- 7 istatistik kartı `auto-fill` grid'de 5+2 diziliyor ve ikinci satır yarım kalıyordu. Orta genişlikte sabit 4 sütun (4+3), çok geniş ekranda 7 sütun yaptık.

### Test
- Dashboard'daki 7 sayıyı doğrudan SQL `COUNT` sorgularıyla karşılaştırdık: birebir aynı.
- Son mesajlardan birine tıklayınca gelen kutusu açıldı ve doğru mesajın modalı kendiliğinden geldi.
- Mobilde (375 px) taşma yok; kartlar 2 sütuna iniyor.
