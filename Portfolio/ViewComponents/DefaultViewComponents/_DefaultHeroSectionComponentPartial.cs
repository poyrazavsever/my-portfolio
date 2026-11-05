using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Models;
using Portfolio.Services;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultHeroSectionComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;
        private readonly ICvStorage _cvStorage;

        public _DefaultHeroSectionComponentPartial(PortfolioContext context, ICvStorage cvStorage)
        {
            _context = context;
            _cvStorage = cvStorage;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Önceki sürümde ViewBag.title / ViewBag.subTitle kullanılıyordu; yazım hatasını derleyici yakalamıyordu.
            // Artık strongly-typed bir model var.
            var hero = await _context.HeroSections.AsNoTracking().FirstOrDefaultAsync();
            var about = await _context.Abouts.AsNoTracking().FirstOrDefaultAsync();
            var contact = await _context.Contacts.AsNoTracking().FirstOrDefaultAsync();
            var hasCv = await SiteQueries.HasCvAsync(_context, _cvStorage);

            var model = new HeroViewModel
            {
                Name = string.IsNullOrWhiteSpace(hero?.Title) ? "Poyraz Avsever" : hero.Title,
                Role = hero?.SubTitle,
                Intro = hero?.Description,
                ImageUrl = hero?.ImageUrl,
                Location = contact?.Location ?? about?.Location,
                OpenToWork = about?.OpenToWork ?? false,
                Email = contact?.Email ?? about?.Email,
                Linkedin = contact?.Linkedin,
                Github = contact?.Github,
                HasCv = hasCv,
            };

            return View(model);
        }
    }
}
