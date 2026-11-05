using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Areas.Admin.Models;
using Portfolio.Context;
using Portfolio.Infrastructure;
using Portfolio.Entities;
using Portfolio.Services;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly IImageStorage _imageStorage;

        public ProfileController(PortfolioContext context, IImageStorage imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await LoadAsync());
        }

        // [Bind(Prefix = "Hero")]: formdaki alanlar "Hero.Title" gibi adlandırılmış (asp-for="Hero.Title").
        // Prefix, model binder'a "bu parametreyi Hero. ile başlayan alanlardan doldur" der.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHero([Bind(Prefix = "Hero")] HeroSection hero, IFormFile? profileImage)
        {
            string? newImageUrl = null;
            if (profileImage is not null)
            {
                var upload = await _imageStorage.SaveAsync(profileImage, "profile");
                if (upload.Success)
                    newImageUrl = upload.Url;
                else
                    ModelState.AddModelError("Hero.ImageUrl", upload.Error!);
            }

            if (!ModelState.IsValid)
            {
                _imageStorage.Delete(newImageUrl);
                var model = await LoadAsync();
                model.Hero = hero;
                return View(nameof(Index), model);
            }

            // Upsert: tek satırlık tablo; kayıt yoksa ekle, varsa güncelle.
            var existing = await _context.HeroSections.FirstOrDefaultAsync();
            if (existing is null)
            {
                hero.ImageUrl = newImageUrl;
                _context.HeroSections.Add(hero);
            }
            else
            {
                existing.Title = hero.Title;
                existing.SubTitle = hero.SubTitle;
                existing.Description = hero.Description;

                if (newImageUrl is not null)
                {
                    _imageStorage.Delete(existing.ImageUrl); // eski görsel diskte yetim kalmasın
                    existing.ImageUrl = newImageUrl;
                }
            }

            await _context.SaveChangesAsync();
            TempData.Success("Ana sayfa bilgileri kaydedildi.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact([Bind(Prefix = "Contact")] Contact contact)
        {
            if (!ModelState.IsValid)
            {
                var model = await LoadAsync();
                model.Contact = contact;
                return View(nameof(Index), model);
            }

            var existing = await _context.Contacts.FirstOrDefaultAsync();
            if (existing is null)
            {
                _context.Contacts.Add(contact);
            }
            else
            {
                existing.Description = contact.Description;
                existing.SubDescription = contact.SubDescription;
                existing.Email = contact.Email;
                existing.Location = contact.Location;
                existing.Linkedin = contact.Linkedin;
                existing.Github = contact.Github;
            }

            await _context.SaveChangesAsync();
            TempData.Success("İletişim bilgileri kaydedildi.");
            return RedirectToAction(nameof(Index));
        }

        private async Task<ProfileViewModel> LoadAsync()
        {
            return new ProfileViewModel
            {
                Hero = await _context.HeroSections.AsNoTracking().FirstOrDefaultAsync() ?? new HeroSection(),
                Contact = await _context.Contacts.AsNoTracking().FirstOrDefaultAsync() ?? new Contact(),
            };
        }
    }
}
