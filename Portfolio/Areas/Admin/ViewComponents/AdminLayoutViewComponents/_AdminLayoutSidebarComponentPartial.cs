using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Areas.Admin.Models;
using Portfolio.Context;

namespace Portfolio.Areas.Admin.ViewComponents.AdminLayoutViewComponents
{
    public class _AdminLayoutSidebarComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _AdminLayoutSidebarComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool mobile = false)
        {
            // Okunmamış mesaj sayısı her sayfada sidebar'da görünür.
            // View Component'in controller'dan bağımsız kendi verisini çekebilmesi tam olarak bu iş için.
            var unreadCount = await _context.Messages.CountAsync(message => !message.IsRead);

            // IsReady = false olan modüller sonraki fazlarda açılacak.
            var model = new AdminSidebarViewModel
            {
                IsMobile = mobile,
                ActiveController = ViewContext.RouteData.Values["controller"]?.ToString(),
                Items =
                [
                    new("Dashboard", "bi-grid-1x2", "Dashboard", "Index"),
                    AdminNavItem.Separator(),
                    new("Profil", "bi-person-vcard", "Profile", "Index"),
                    new("Hakkımda", "bi-person", "About", "Index"),
                    new("Eğitim", "bi-mortarboard", "Education", "EducationList"),
                    new("Deneyim", "bi-briefcase", "Experience", "ExperienceList"),
                    new("Yetenekler", "bi-cpu", "Skill", "SkillList"),
                    new("Projeler", "bi-layers", "Project", "ProjectList"),
                    new("Kategoriler", "bi-tags", "Category", "CategoryList"),
                    AdminNavItem.Separator(),
                    new("Mesajlar", "bi-chat-square-text", "Message", "MessageList", Badge: unreadCount),
                    AdminNavItem.Separator(),
                    new("Ayarlar", "bi-sliders", "Setting", "Index"),
                    new("Portfolyoyu aç", "bi-display", ExternalUrl: "/"),
                ]
            };

            return View(model);
        }
    }
}
