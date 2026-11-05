using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Areas.Admin.Models;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Services;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ICvStorage _cvStorage;

        public DashboardController(PortfolioContext context, ICvStorage cvStorage)
        {
            _context = context;
            _cvStorage = cvStorage;
        }

        public async Task<IActionResult> Index()
        {
            // Bütün sayılar DB'den gelir (case: "veriler sabit sayılarla değil, EF Core sorgularıyla üretilmeli").
            // Her CountAsync SQL'de tek bir SELECT COUNT(*) olur; satırların kendisi belleğe çekilmez.
            var model = new DashboardViewModel
            {
                ProjectCount = await _context.Projects.CountAsync(),
                ActiveProjectCount = await _context.Projects.CountAsync(project => project.IsActive),
                SkillCount = await _context.Skills.CountAsync(),
                ExperienceCount = await _context.Experiences.CountAsync(),
                EducationCount = await _context.Educations.CountAsync(),
                MessageCount = await _context.Messages.CountAsync(),
                UnreadMessageCount = await _context.Messages.CountAsync(message => !message.IsRead),

                // OrderByDescending + Take: "son 5" -> SQL'de ORDER BY ... DESC + TOP(5)
                LatestMessages = await _context.Messages
                    .AsNoTracking()
                    .OrderByDescending(message => message.SendDate)
                    .Take(5)
                    .ToListAsync(),

                // Include: kategori adını göstermek için ilişkili tabloyu da getir (JOIN).
                LatestProjects = await _context.Projects
                    .AsNoTracking()
                    .Include(project => project.Category)
                    .OrderByDescending(project => project.CreatedDate)
                    .Take(3)
                    .ToListAsync(),

                // GroupBy: projeleri kategoriye göre grupla, her grup için sayı üret.
                // EF bunu SQL'de GROUP BY + COUNT olarak çalıştırır; gruplama DB'de yapılır.
                CategorySummary = await _context.Projects
                    .AsNoTracking()
                    .GroupBy(project => project.Category!.CategoryName)
                    .Select(group => new CategorySummaryItem
                    {
                        CategoryName = group.Key,
                        ProjectCount = group.Count(),
                        ActiveCount = group.Count(project => project.IsActive),
                    })
                    .OrderByDescending(item => item.ProjectCount)
                    .ThenBy(item => item.CategoryName)
                    .ToListAsync(),

                HasCv = await SiteQueries.HasCvAsync(_context, _cvStorage),
            };

            return View(model);
        }
    }
}
