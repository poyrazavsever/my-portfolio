using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Entities;
using Portfolio.Infrastructure;
using Portfolio.Services;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ICvStorage _cvStorage;

        public SettingController(PortfolioContext context, ICvStorage cvStorage)
        {
            _context = context;
            _cvStorage = cvStorage;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var setting = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync() ?? new SiteSetting();
            LoadCvInfo(setting);
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSetting setting)
        {
            if (!ModelState.IsValid)
            {
                LoadCvInfo(setting);
                return View(setting);
            }

            var existing = await GetOrCreateAsync();
            // Yalnızca formdaki alanı güncelliyoruz; CV alanlarına bu formdan dokunulmaz.
            existing.SiteTitle = setting.SiteTitle;

            await _context.SaveChangesAsync();
            TempData.Success("Ayarlar kaydedildi.");
            return RedirectToAction(nameof(Index));
        }

        // RequestSizeLimit: 5 MB'lık sınırın biraz üstü. Çok büyük bir istek, action'a hiç ulaşmadan
        // sunucu tarafından reddedilir (bellek ve disk korunur).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadCv(IFormFile? cvFile, string? downloadName)
        {
            if (cvFile is null || cvFile.Length == 0)
            {
                TempData.Error("Lütfen bir PDF dosyası seçin.");
                return RedirectToAction(nameof(Index));
            }

            var result = await _cvStorage.SaveAsync(cvFile);
            if (!result.Success)
            {
                TempData.Error(result.Error!);
                return RedirectToAction(nameof(Index));
            }

            var setting = await GetOrCreateAsync();
            var oldPath = setting.CvFilePath;

            setting.CvFilePath = result.RelativePath;
            setting.CvDownloadName = await BuildDownloadNameAsync(downloadName);
            setting.CvUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Eski dosyayı ancak yeni kayıt başarıyla kaydedildikten sonra sil.
            _cvStorage.Delete(oldPath);

            TempData.Success("CV yüklendi. Sitedeki \"CV indir\" butonları artık görünür.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCv()
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync();
            if (setting?.CvFilePath is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var oldPath = setting.CvFilePath;
            setting.CvFilePath = null;
            setting.CvDownloadName = null;
            setting.CvUpdatedAt = null;
            await _context.SaveChangesAsync();
            _cvStorage.Delete(oldPath);

            TempData.Success("CV kaldırıldı. Sitedeki \"CV indir\" butonları gizlendi.");
            return RedirectToAction(nameof(Index));
        }

        // Admin'in yüklediği dosyayı tarayıcıda kontrol edebilmesi için (indirmek yerine yeni sekmede açar).
        [HttpGet]
        public async Task<IActionResult> PreviewCv()
        {
            var path = await _context.SiteSettings.AsNoTracking().Select(s => s.CvFilePath).FirstOrDefaultAsync();
            var fullPath = _cvStorage.GetFullPath(path);
            return fullPath is null ? NotFound() : PhysicalFile(fullPath, "application/pdf");
        }

        private async Task<SiteSetting> GetOrCreateAsync()
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync();
            if (setting is null)
            {
                setting = new SiteSetting();
                _context.SiteSettings.Add(setting);
            }

            return setting;
        }

        // Ziyaretçinin indireceği dosyanın adı: verilmediyse "Ad-Soyad-CV.pdf".
        // Dosya adında sorun çıkaracak karakterleri temizliyoruz.
        private async Task<string> BuildDownloadNameAsync(string? requested)
        {
            var name = requested?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                var owner = await _context.HeroSections.AsNoTracking().Select(h => h.Title).FirstOrDefaultAsync() ?? "CV";
                name = $"{owner.Replace(' ', '-')}-CV";
            }

            var invalid = Path.GetInvalidFileNameChars();
            name = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^4];
            }

            return $"{(name.Length > 140 ? name[..140] : name)}.pdf";
        }

        private void LoadCvInfo(SiteSetting setting)
        {
            var fullPath = _cvStorage.GetFullPath(setting.CvFilePath);
            ViewData["CvExists"] = fullPath is not null;
            ViewData["CvSizeKb"] = fullPath is null ? 0 : (int)Math.Ceiling(new FileInfo(fullPath).Length / 1024d);
            ViewData["CvMissingFile"] = setting.CvFilePath is not null && fullPath is null;
        }
    }
}
