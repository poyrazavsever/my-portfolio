using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Entities;
using Portfolio.Models;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultContactComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultContactComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        /// <param name="form">
        /// Doğrulama hatasıyla dönüldüğünde controller kullanıcının yazdıklarını buraya verir.
        /// View Component, sayfanın ModelState'ini paylaştığı için hata mesajları da görünür.
        /// </param>
        public async Task<IViewComponentResult> InvokeAsync(Message? form = null)
        {
            var info = await _context.Contacts.AsNoTracking().FirstOrDefaultAsync() ?? new Contact();
            return View(new ContactSectionViewModel { Info = info, Form = form ?? new Message() });
        }
    }
}
