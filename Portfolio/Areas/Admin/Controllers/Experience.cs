using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;

namespace Portfolio.Areas.Admin.Controllers
{

    [Area("Admin")]
    public class Experience : Controller
    {
        private readonly PortfolioContext _context;

        public Experience(PortfolioContext context)
        {
            _context = context;
        }

        public IActionResult ExperienceList()
        {

            var values = _context.Experiences.ToList();

            return View(values);
        }
    }
}
