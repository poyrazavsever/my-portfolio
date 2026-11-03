using Microsoft.AspNetCore.Mvc;

using Portfolio.Context;
using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MessageController : Controller
    {

        private readonly PortfolioContext _context;

        public MessageController(PortfolioContext context)
        {
            _context = context;
        }

        public IActionResult MessageList()
        {
            var values = _context.Messages.ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult CreateMessage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMessage(Message message)
        {
            if (!ModelState.IsValid)
            {
                return View(message);
            }

            message.SendDate = DateTime.Now;
            message.IsRead = false;
            _context.Messages.Add(message);
            _context.SaveChanges();
            return RedirectToAction(nameof(MessageList));
        }

        public IActionResult DeleteMessage(int id)
        {
            var values = _context.Messages.Find(id);
            if (values is null)
            {
                return NotFound();
            }
            _context.Messages.Remove(values);
            _context.SaveChanges();
            return RedirectToAction(nameof(MessageList));
        }

        public IActionResult UpdateMessage(int id)
        {
            var values = _context.Messages.Find(id);
            if (values is null)
            {
                return NotFound();
            }
            return View(values);
        }

        [HttpPost]
        public IActionResult UpdateMessage(Message message)
        {
            message.SendDate = DateTime.Now;
            message.IsRead = true;
            _context.Messages.Update(message);
            _context.SaveChanges();
            return RedirectToAction(nameof(MessageList));
        }
    }
}
