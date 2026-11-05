using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;
using Portfolio.Services;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProjectController : Controller
    {
        private const string ImageFolder = "projects";

        private readonly PortfolioContext _context;
        private readonly IImageStorage _imageStorage;

        public ProjectController(PortfolioContext context, IImageStorage imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<IActionResult> ProjectList(int? categoryId)
        {
            // Include: Project tablosuyla birlikte ilişkili Category satırını da getir (SQL'de JOIN).
            // Olmasaydı project.Category null gelirdi; EF ilişkili veriyi kendiliğinden yüklemez.
            var query = _context.Projects
                .AsNoTracking()
                .Include(project => project.Category)
                .AsQueryable();

            // Sorgu ToListAsync çağrılana kadar çalışmaz; Where'i koşullu ekleyebiliriz.
            if (categoryId is not null)
            {
                query = query.Where(project => project.CategoryId == categoryId);
            }

            var values = await query
                .OrderByDescending(project => project.CreatedDate)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            return View(values);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            if (!await _context.Categories.AnyAsync())
            {
                TempData.Error("Proje eklemeden önce en az bir kategori oluşturmalısınız.");
                return RedirectToAction("CreateCategory", "Category");
            }

            await LoadCategoriesAsync();
            return View(new Project());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project project, IFormFile? coverImage)
        {
            var newImageUrl = await TrySaveImageAsync(coverImage);

            if (!ModelState.IsValid)
            {
                _imageStorage.Delete(newImageUrl); // form geçersizse yüklenen dosya boşa kalmasın
                await LoadCategoriesAsync(project.CategoryId);
                return View(project);
            }

            project.CoverImageUrl = newImageUrl;
            project.CreatedDate = DateTime.Now;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData.Success($"\"{project.Title}\" eklendi.");
            return RedirectToAction(nameof(ProjectList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project is null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync(project.CategoryId);
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(Project project, IFormFile? coverImage, bool removeImage = false)
        {
            var existing = await _context.Projects.FindAsync(project.ProjectId);
            if (existing is null)
            {
                return NotFound();
            }

            var newImageUrl = await TrySaveImageAsync(coverImage);

            if (!ModelState.IsValid)
            {
                _imageStorage.Delete(newImageUrl);
                project.CoverImageUrl = existing.CoverImageUrl; // önizleme mevcut görseli göstermeye devam etsin
                await LoadCategoriesAsync(project.CategoryId);
                return View(project);
            }

            existing.Title = project.Title;
            existing.Description = project.Description;
            existing.GithubUrl = project.GithubUrl;
            existing.CategoryId = project.CategoryId;
            existing.IsActive = project.IsActive;

            if (newImageUrl is not null || removeImage)
            {
                _imageStorage.Delete(existing.CoverImageUrl);
                existing.CoverImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();
            TempData.Success("Proje güncellendi.");
            return RedirectToAction(nameof(ProjectList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project is null)
            {
                return NotFound();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            _imageStorage.Delete(project.CoverImageUrl); // kayıt silindikten sonra dosyayı da temizle

            TempData.Success("Proje silindi.");
            return RedirectToAction(nameof(ProjectList));
        }

        private async Task<string?> TrySaveImageAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return null;
            }

            var result = await _imageStorage.SaveAsync(file, ImageFolder);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(Project.CoverImageUrl), result.Error!);
            }

            return result.Url;
        }

        // SelectList: dropdown için (değer, görünen metin, seçili değer) listesi.
        // View tarafında <select asp-items="ViewBag.Categories"> ile kullanılır.
        private async Task LoadCategoriesAsync(int? selectedId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, nameof(Category.CategoryId), nameof(Category.CategoryName), selectedId);
        }
    }
}
