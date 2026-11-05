using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultSkillComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultSkillComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await _context.Skills
                .AsNoTracking()
                .Where(skill => skill.IsActive)
                .OrderBy(skill => skill.DisplayOrder)
                .ThenByDescending(skill => skill.Level)
                .ToListAsync();

            return values.Count == 0 ? Content(string.Empty) : View(values);
        }
    }
}
