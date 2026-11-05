using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Areas.Admin.Controllers
{
    // Faz 9'da DashboardViewModel ile istatistik kartları, son mesajlar ve kategori özeti gelecek.
    [Area("Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
