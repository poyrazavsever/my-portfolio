using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    // Faz 8'de CV yükleme (yalnızca PDF) bu sayfaya eklenecek.
    [Area("Admin")]
    public class SettingController : Controller
    {
        private readonly PortfolioContext _context;

        public SettingController(PortfolioContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var setting = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync() ?? new SiteSetting();
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSetting setting)
        {
            if (!ModelState.IsValid)
            {
                return View(setting);
            }

            var existing = await _context.SiteSettings.FirstOrDefaultAsync();
            if (existing is null)
            {
                _context.SiteSettings.Add(new SiteSetting { SiteTitle = setting.SiteTitle });
            }
            else
            {
                // Yalnızca formdaki alanı güncelliyoruz; CV alanlarına bu formdan dokunulmaz.
                existing.SiteTitle = setting.SiteTitle;
            }

            await _context.SaveChangesAsync();
            TempData.Success("Ayarlar kaydedildi.");
            return RedirectToAction(nameof(Index));
        }
    }
}
