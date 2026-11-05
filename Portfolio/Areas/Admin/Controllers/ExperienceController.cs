using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;
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

        public IActionResult ExperienceList()
        {

            // En güncel deneyim en üstte; devam edenler (EndDate = null) kendi başlangıç tarihine göre sıralanır.
            var values = _context.Experiences
                .OrderByDescending(experience => experience.StartDate)
                .ToList();

            return View(values);
        }

        [HttpGet]
        public IActionResult CreateExperience()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateExperience(Experience experience)
        {
            if (!ModelState.IsValid)
            {
                return View(experience);
            }

            _context.Experiences.Add(experience);
            _context.SaveChanges();
            return RedirectToAction("ExperienceList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExperience(int id)
        {
            var values = _context.Experiences.Find(id);

            if (values is null)
            {
                return NotFound();
            }

            _context.Experiences.Remove(values);
            _context.SaveChanges();
            return RedirectToAction("ExperienceList");
        }

        [HttpGet]
        public IActionResult UpdateExperience(int id)
        {
            var values = _context.Experiences.Find(id);

            if (values is null)
            {
                return NotFound();
            }

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateExperience(Experience experience)
        {
            if (!ModelState.IsValid)
            {
                return View(experience);
            }

            var values = _context.Experiences.Find(experience.ExperienceId);

            if (values is null)
            {
                return NotFound();
            }

            values.Title = experience.Title;
            values.CompanyName = experience.CompanyName;
            values.Description = experience.Description;
            values.SideTitle = experience.SideTitle;
            values.WorkType = experience.WorkType;
            values.StartDate = experience.StartDate;
            values.EndDate = experience.EndDate;
            values.IsActive = experience.IsActive;

            _context.SaveChanges();
            return RedirectToAction(nameof(ExperienceList));
        }
    }
}
