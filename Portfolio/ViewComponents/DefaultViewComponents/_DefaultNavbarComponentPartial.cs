using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Services;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultNavbarComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;
        private readonly ICvStorage _cvStorage;

        public _DefaultNavbarComponentPartial(PortfolioContext context, ICvStorage cvStorage)
        {
            _context = context;
            _cvStorage = cvStorage;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View(await SiteQueries.LoadChromeAsync(_context, _cvStorage));
        }
    }
}
