using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExperienceController : Controller
    {
        private readonly PortfolioContext _context;

        public ExperienceController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ExperienceList()
        {
            // En güncel deneyim en üstte.
            var values = await _context.Experiences
                .AsNoTracking()
                .OrderByDescending(experience => experience.StartDate)
                .ToListAsync();

            return View(values);
        }

        [HttpGet]
        public IActionResult CreateExperience()
        {
            return View(new Experience { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExperience(Experience experience)
        {
            ValidateDates(experience);
            if (!ModelState.IsValid)
            {
                return View(experience);
            }

            _context.Experiences.Add(experience);
            await _context.SaveChangesAsync();
            TempData.Success("Deneyim eklendi.");
            return RedirectToAction(nameof(ExperienceList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateExperience(int id)
        {
            var experience = await _context.Experiences.FindAsync(id);
            return experience is null ? NotFound() : View(experience);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExperience(Experience experience)
        {
            ValidateDates(experience);
            if (!ModelState.IsValid)
            {
                return View(experience);
            }

            var existing = await _context.Experiences.FindAsync(experience.ExperienceId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.Title = experience.Title;
            existing.CompanyName = experience.CompanyName;
            existing.Description = experience.Description;
            existing.SideTitle = experience.SideTitle;
            existing.WorkType = experience.WorkType;
            existing.StartDate = experience.StartDate;
            existing.EndDate = experience.EndDate;
            existing.IsActive = experience.IsActive;

            await _context.SaveChangesAsync();
            TempData.Success("Deneyim güncellendi.");
            return RedirectToAction(nameof(ExperienceList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            var experience = await _context.Experiences.FindAsync(id);
            if (experience is null)
            {
                return NotFound();
            }

            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();
            TempData.Success("Deneyim silindi.");
            return RedirectToAction(nameof(ExperienceList));
        }

        private void ValidateDates(Experience experience)
        {
            if (experience.EndDate is not null && experience.EndDate < experience.StartDate)
            {
                ModelState.AddModelError(nameof(Experience.EndDate), "Bitiş tarihi başlangıçtan önce olamaz.");
            }
        }
    }
}
