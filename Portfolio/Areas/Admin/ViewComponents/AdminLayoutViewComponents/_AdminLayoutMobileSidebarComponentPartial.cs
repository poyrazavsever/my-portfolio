using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Areas.Admin.ViewComponents.AdminLayoutViewComponents
{
    public class _AdminLayoutMobileSidebarComponentPartial : ViewComponent
    {
    
        public IViewComponentResult Invoke()
        {
            return View();
        }

    }
}
