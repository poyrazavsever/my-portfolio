using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultAboutComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultAboutComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var about = await _context.Abouts.AsNoTracking().FirstOrDefaultAsync();

            // Admin henüz Hakkımda girmediyse bölüm hiç çizilmez (boş başlık göstermek yerine).
            return about is null ? Content(string.Empty) : View(about);
        }
    }
}
