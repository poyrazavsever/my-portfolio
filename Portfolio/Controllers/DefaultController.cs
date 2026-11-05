using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Entities;
using Portfolio.Infrastructure;
using Portfolio.Services;

namespace Portfolio.Controllers
{
    public class DefaultController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ICvStorage _cvStorage;

        public DefaultController(PortfolioContext context, ICvStorage cvStorage)
        {
            _context = context;
            _cvStorage = cvStorage;
        }

        public IActionResult Index()
        {
            return View();
        }

        // CV dosyası wwwroot dışında (App_Data/cv). Ziyaretçi dosyaya yalnızca bu action üzerinden ulaşır:
        // indirilen adı ("Poyraz-Avsever-CV.pdf") ve içerik türünü biz belirleriz, diskteki Guid'li ad hiç görünmez.
        [HttpGet]
        public async Task<IActionResult> DownloadCv()
        {
            var setting = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
            var fullPath = _cvStorage.GetFullPath(setting?.CvFilePath);

            if (fullPath is null)
            {
                return NotFound();
            }

            // fileDownloadName verildiğinde ASP.NET "Content-Disposition: attachment" ekler: tarayıcı dosyayı indirir.
            return PhysicalFile(fullPath, "application/pdf", setting!.CvDownloadName ?? "CV.pdf");
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
