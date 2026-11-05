using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultHeroSectionComponentPartial : ViewComponent
    {

        private readonly PortfolioContext _context;

        public _DefaultHeroSectionComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var values = _context.HeroSections.FirstOrDefault();

            ViewBag.title = values?.Title;
            ViewBag.subTitle = values?.SubTitle;
            ViewBag.description = values?.Description;
            ViewBag.imageUrl = values?.ImageUrl;

            return View();
        }

    }
}
