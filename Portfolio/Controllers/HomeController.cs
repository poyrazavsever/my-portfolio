using Microsoft.AspNetCore.Mvc;
using Portfolio.Models;
using System.Diagnostics;

namespace Portfolio.Controllers
{
    // Şablondan kalan Index/Privacy sayfaları kaldırıldı; portfolyo DefaultController'da.
    // Bu controller yalnızca üretim ortamındaki hata sayfasını (UseExceptionHandler) sunar.
    public class HomeController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
