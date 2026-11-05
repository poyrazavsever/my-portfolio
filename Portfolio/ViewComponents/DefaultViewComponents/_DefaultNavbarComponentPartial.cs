using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;
using Portfolio.Infrastructure;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultNavbarComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultNavbarComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View(await SiteQueries.LoadChromeAsync(_context));
        }
    }
}
