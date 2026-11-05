using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Entities;
using Portfolio.Infrastructure;

namespace Portfolio.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CertificateController : Controller
    {
        private readonly PortfolioContext _context;

        public CertificateController(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CertificateList()
        {
            var values = await _context.Certificates
                .AsNoTracking()
                .OrderByDescending(certificate => certificate.IssueDate)
                .ToListAsync();

            return View(values);
        }

        [HttpGet]
        public IActionResult CreateCertificate()
        {
            return View(new Certificate { IssueDate = DateOnly.FromDateTime(DateTime.Today) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCertificate(Certificate certificate)
        {
            if (!ModelState.IsValid)
            {
                return View(certificate);
            }

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            TempData.Success("Sertifika eklendi.");
            return RedirectToAction(nameof(CertificateList));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCertificate(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            return certificate is null ? NotFound() : View(certificate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCertificate(Certificate certificate)
        {
            if (!ModelState.IsValid)
            {
                return View(certificate);
            }

            var existing = await _context.Certificates.FindAsync(certificate.CertificateId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.Title = certificate.Title;
            existing.Issuer = certificate.Issuer;
            existing.IssueDate = certificate.IssueDate;
            existing.CredentialUrl = certificate.CredentialUrl;
            existing.IsActive = certificate.IsActive;

            await _context.SaveChangesAsync();
            TempData.Success("Sertifika güncellendi.");
            return RedirectToAction(nameof(CertificateList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate is null)
            {
                return NotFound();
            }

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
            TempData.Success("Sertifika silindi.");
            return RedirectToAction(nameof(CertificateList));
        }
    }
}
