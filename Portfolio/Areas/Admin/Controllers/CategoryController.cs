using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Areas.Admin.Models;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly PortfolioContext _context;

        public CategoryController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CategoryList()
        {
            // Projeksiyon: Include ile bütün projeleri çekmek yerine DB'ye sadece sayıları sorduruyoruz.
            // EF bunu tek bir SQL'e çevirir: SELECT ..., (SELECT COUNT(*) FROM Projects WHERE ...) ...
            var values = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .Select(category => new CategoryListItemViewModel
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    ProjectCount = category.Projects.Count,
                    ActiveProjectCount = category.Projects.Count(project => project.IsActive),
                })
                .ToListAsync();

            return View(values);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            await ValidateUniqueNameAsync(category);
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            TempData.Success($"\"{category.CategoryName}\" kategorisi eklendi.");
            return RedirectToAction(nameof(CategoryList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return category is null ? NotFound() : View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(Category category)
        {
            await ValidateUniqueNameAsync(category);
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var existing = await _context.Categories.FindAsync(category.CategoryId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.CategoryName = category.CategoryName;
            await _context.SaveChangesAsync();
            TempData.Success("Kategori güncellendi.");
            return RedirectToAction(nameof(CategoryList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound();
            }

            // Önce kendimiz kontrol edip kullanıcıya anlaşılır bir mesaj veriyoruz...
            var projectCount = await _context.Projects.CountAsync(project => project.CategoryId == id);
            if (projectCount > 0)
            {
                TempData.Error($"\"{category.CategoryName}\" kategorisinde {projectCount} proje var. " +
                                    "Önce projeleri başka bir kategoriye taşıyın veya silin.");
                return RedirectToAction(nameof(CategoryList));
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // ...ama son söz DB'de: kontrolden hemen sonra başka biri proje eklediyse Restrict kuralı silmeyi reddeder.
                TempData.Error("Kategori, bağlı projeleri olduğu için silinemedi.");
                return RedirectToAction(nameof(CategoryList));
            }

            TempData.Success("Kategori silindi.");
            return RedirectToAction(nameof(CategoryList));
        }

        private async Task ValidateUniqueNameAsync(Category category)
        {
            var name = category.CategoryName?.Trim() ?? string.Empty;
            category.CategoryName = name;

            var exists = await _context.Categories.AnyAsync(other =>
                other.CategoryName == name && other.CategoryId != category.CategoryId);

            if (exists)
            {
                ModelState.AddModelError(nameof(Category.CategoryName), "Bu adda bir kategori zaten var.");
            }
        }
    }
}
