using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Areas.Admin.Models;
using Portfolio.Context;
using Portfolio.Infrastructure;

namespace Portfolio.Areas.Admin.Controllers
{
    // Gelen kutusu. Mesajın orijinal içeriği (ad, e-posta, konu, metin) bilinçli olarak düzenlenemez:
    // önceki "Düzenle" ve "Test mesajı" ekranları kaldırıldı. Yalnızca okundu durumu değişir ve mesaj silinebilir.
    [Area("Admin")]
    public class MessageController : Controller
    {
        private readonly PortfolioContext _context;

        public MessageController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MessageList(string? q, string status = "all", string sort = "newest", int? open = null)
        {
            var query = _context.Messages.AsNoTracking();

            // Arama: gönderen adı, e-posta veya konu. EF bunu SQL'de LIKE '%...%' sorgusuna çevirir.
            var term = q?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(message =>
                    message.NameSurname.Contains(term) ||
                    message.Email.Contains(term) ||
                    message.Subject.Contains(term));
            }

            query = status switch
            {
                "unread" => query.Where(message => !message.IsRead),
                "read" => query.Where(message => message.IsRead),
                _ => query,
            };

            query = sort == "oldest"
                ? query.OrderBy(message => message.SendDate)
                : query.OrderByDescending(message => message.SendDate);

            var model = new MessageListViewModel
            {
                Messages = await query.ToListAsync(),
                Query = term,
                Status = status is "unread" or "read" ? status : "all",
                Sort = sort == "oldest" ? "oldest" : "newest",
                TotalCount = await _context.Messages.CountAsync(),
                UnreadCount = await _context.Messages.CountAsync(message => !message.IsRead),
                OpenId = open,
            };

            return View(model);
        }

        // Modal açılınca JS (fetch) ile çağrılır. Sayfa yenilenmez; JSON döner.
        // Antiforgery token'ı form alanı yerine "RequestVerificationToken" header'ında gelir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message is null)
            {
                return NotFound();
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var unread = await _context.Messages.CountAsync(other => !other.IsRead);
            return Json(new { id, isRead = true, unread });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRead(int id, string? returnUrl)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message is null)
            {
                return NotFound();
            }

            message.IsRead = !message.IsRead;
            await _context.SaveChangesAsync();

            TempData.Success(message.IsRead ? "Mesaj okundu olarak işaretlendi." : "Mesaj okunmadı olarak işaretlendi.");
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id, string? returnUrl)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message is null)
            {
                return NotFound();
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData.Success("Mesaj silindi.");
            return RedirectToLocal(returnUrl);
        }

        // Filtreli listeye geri dönmek için returnUrl kullanıyoruz. Ama dışarıdan gelen bir adrese
        // körü körüne yönlendirmek "open redirect" açığıdır (?returnUrl=https://kotu-site.com).
        // Url.IsLocalUrl yalnızca bu sitenin içindeki adreslere izin verir.
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            return Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(MessageList));
        }
    }
}
