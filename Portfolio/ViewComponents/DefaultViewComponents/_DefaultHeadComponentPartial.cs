using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Models;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultHeadComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultHeadComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var siteTitle = await _context.SiteSettings.AsNoTracking()
                .Select(setting => setting.SiteTitle)
                .FirstOrDefaultAsync();

            var description = await _context.HeroSections.AsNoTracking()
                .Select(hero => hero.Description)
                .FirstOrDefaultAsync();

            // Sayfa kendi başlığını verdiyse (ör. hata sayfası) onu öne koy.
            var pageTitle = ViewData["Title"] as string;
            var baseTitle = string.IsNullOrWhiteSpace(siteTitle) ? "Poyraz Avsever" : siteTitle;
            var title = string.IsNullOrWhiteSpace(pageTitle) ? baseTitle : $"{pageTitle} · {baseTitle}";

            return View(new HeadViewModel(title, description));
        }
    }
}
