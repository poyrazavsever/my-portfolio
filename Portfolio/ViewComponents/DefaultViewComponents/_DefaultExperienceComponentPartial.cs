using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultExperienceComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultExperienceComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Pasif kayıtlar sitede görünmez; en güncel deneyim en üstte.
            var values = await _context.Experiences
                .AsNoTracking()
                .Where(experience => experience.IsActive)
                .OrderByDescending(experience => experience.StartDate)
                .ToListAsync();

            return values.Count == 0 ? Content(string.Empty) : View(values);
        }
    }
}
