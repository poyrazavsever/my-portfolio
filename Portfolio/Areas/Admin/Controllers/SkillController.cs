using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SkillController : Controller
    {
        private readonly PortfolioContext _context;

        public SkillController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> SkillList()
        {
            // Önce elle verilen sıra, eşitlikte seviye yüksek olan önde.
            var values = await _context.Skills
                .AsNoTracking()
                .OrderBy(skill => skill.DisplayOrder)
                .ThenByDescending(skill => skill.Level)
                .ToListAsync();

            return View(values);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSkill()
        {
            // Yeni yetenek varsayılan olarak listenin sonuna eklensin.
            var nextOrder = (await _context.Skills.MaxAsync(skill => (int?)skill.DisplayOrder) ?? 0) + 1;
            return View(new Skill { Level = 50, DisplayOrder = nextOrder, IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSkill(Skill skill)
        {
            if (!ModelState.IsValid)
            {
                return View(skill);
            }

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            TempData.Success("Yetenek eklendi.");
            return RedirectToAction(nameof(SkillList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            return skill is null ? NotFound() : View(skill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkill(Skill skill)
        {
            if (!ModelState.IsValid)
            {
                return View(skill);
            }

            var existing = await _context.Skills.FindAsync(skill.SkillId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.Name = skill.Name;
            existing.Level = skill.Level;
            existing.DisplayOrder = skill.DisplayOrder;
            existing.IsActive = skill.IsActive;

            await _context.SaveChangesAsync();
            TempData.Success("Yetenek güncellendi.");
            return RedirectToAction(nameof(SkillList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill is null)
            {
                return NotFound();
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();
            TempData.Success("Yetenek silindi.");
            return RedirectToAction(nameof(SkillList));
        }
    }
}
