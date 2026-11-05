using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultEducationComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultEducationComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await _context.Educations
                .AsNoTracking()
                .Where(education => education.IsActive)
                .OrderByDescending(education => education.StartDate)
                .ToListAsync();

            return values.Count == 0 ? Content(string.Empty) : View(values);
        }
    }
}
