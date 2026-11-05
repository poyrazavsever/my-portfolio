using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Models;

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
            var projects = await _context.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .Where(project => project.IsActive)
                .OrderByDescending(project => project.CreatedDate)
                .ToListAsync();

            if (projects.Count == 0)
            {
                return Content(string.Empty);
            }

            // Filtre butonları: yalnızca en az bir aktif projesi olan kategoriler.
            var categories = projects
                .Select(project => project.Category!)
                .DistinctBy(category => category.CategoryId)
                .OrderBy(category => category.CategoryName)
                .ToList();

            return View(new ProjectSectionViewModel { Projects = projects, Categories = categories });
        }
    }
}
