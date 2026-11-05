using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultProjectComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;


        public _DefaultProjectComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await _context.Categories
                .AsNoTracking()
                .Where(category => category.Projects.Any())
                .Include(category => category.Projects.OrderBy(project => project.ProjectId))
                .OrderBy(category => category.CategoryName)
                .ToListAsync();

            return View(values);
        }
    }
}
