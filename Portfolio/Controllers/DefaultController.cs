using Microsoft.AspNetCore.Mvc;
using Portfolio.Context;
using Portfolio.Entities;
using Portfolio.Infrastructure;

namespace Portfolio.Controllers
{
    public class DefaultController : Controller
    {
        private readonly PortfolioContext _context;

        public DefaultController(PortfolioContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // [Bind(...)]: Formdan YALNIZCA bu dört alan alınır. Message'daki IsRead, SendDate, MessageId
        // gibi alanlar kötü niyetli bir istekle doldurulamaz ("overposting" saldırısı).
        // Prefix = "Form": formdaki alanlar ContactSectionViewModel.Form üzerinden "Form.Email" diye adlandırılmış.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            [Bind(nameof(Message.NameSurname), nameof(Message.Email), nameof(Message.Subject), nameof(Message.MessageDetail), Prefix = "Form")]
            Message message,
            string? website)
        {
            // Honeypot doluysa gönderen büyük ihtimalle bot: kaydetme ama başarılı gibi davran (bota ipucu verme).
            if (!string.IsNullOrEmpty(website))
            {
                TempData.Success("Teşekkürler, mesajınız alındı.");
                return Redirect(Url.Action(nameof(Index)) + "#contact");
            }

            if (!ModelState.IsValid)
            {
                // Sayfayı yazılanlar ve hata mesajlarıyla birlikte yeniden çiz.
                ViewData["ContactForm"] = message;
                return View(nameof(Index));
            }

            message.SendDate = DateTime.Now;
            message.IsRead = false;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData.Success("Teşekkürler, mesajınız alındı. En kısa sürede dönüş yapacağım.");
            return Redirect(Url.Action(nameof(Index)) + "#contact");
        }
    }
}
