using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EducationController : Controller
    {
        private readonly PortfolioContext _context;

        public EducationController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> EducationList()
        {
            var values = await _context.Educations
                .AsNoTracking()
                .OrderByDescending(education => education.StartDate)
                .ToListAsync();

            return View(values);
        }

        [HttpGet]
        public IActionResult CreateEducation()
        {
            return View(new Education { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEducation(Education education)
        {
            ValidateDates(education);
            if (!ModelState.IsValid)
            {
                return View(education);
            }

            _context.Educations.Add(education);
            await _context.SaveChangesAsync();
            TempData.Success("Eğitim eklendi.");
            return RedirectToAction(nameof(EducationList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateEducation(int id)
        {
            var education = await _context.Educations.FindAsync(id);
            return education is null ? NotFound() : View(education);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEducation(Education education)
        {
            ValidateDates(education);
            if (!ModelState.IsValid)
            {
                return View(education);
            }

            var existing = await _context.Educations.FindAsync(education.EducationId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.EducationTitle = education.EducationTitle;
            existing.SubTitle = education.SubTitle;
            existing.Description = education.Description;
            existing.StartDate = education.StartDate;
            existing.EndDate = education.EndDate;
            existing.IsActive = education.IsActive;

            await _context.SaveChangesAsync();
            TempData.Success("Eğitim güncellendi.");
            return RedirectToAction(nameof(EducationList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            var education = await _context.Educations.FindAsync(id);
            if (education is null)
            {
                return NotFound();
            }

            _context.Educations.Remove(education);
            await _context.SaveChangesAsync();
            TempData.Success("Eğitim silindi.");
            return RedirectToAction(nameof(EducationList));
        }

        // Data Annotations tek bir alana bakar; iki alanı karşılaştıran kuralı controller'da ekliyoruz.
        private void ValidateDates(Education education)
        {
            if (education.EndDate is not null && education.EndDate < education.StartDate)
            {
                ModelState.AddModelError(nameof(Education.EndDate), "Bitiş tarihi başlangıçtan önce olamaz.");
            }
        }
    }
}
