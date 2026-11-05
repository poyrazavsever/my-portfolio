using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AboutController : Controller
    {
        private readonly PortfolioContext _context;

        public AboutController(PortfolioContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var about = await _context.Abouts.AsNoTracking().FirstOrDefaultAsync() ?? new About();
            return View(about);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(About about)
        {
            if (!ModelState.IsValid)
            {
                return View(about);
            }

            var existing = await _context.Abouts.FirstOrDefaultAsync();
            if (existing is null)
            {
                _context.Abouts.Add(about);
            }
            else
            {
                existing.NameSurname = about.NameSurname;
                existing.Expert = about.Expert;
                existing.Location = about.Location;
                existing.Email = about.Email;
                existing.Description = about.Description;
                existing.OpenToWork = about.OpenToWork;
            }

            await _context.SaveChangesAsync();
            TempData.Success("Hakkımda bölümü kaydedildi.");
            return RedirectToAction(nameof(Index));
        }
    }
}
