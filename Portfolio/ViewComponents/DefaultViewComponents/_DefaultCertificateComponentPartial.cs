using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;

namespace Portfolio.ViewComponents.DefaultViewComponents
{
    public class _DefaultCertificateComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultCertificateComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await _context.Certificates
                .AsNoTracking()
                .Where(certificate => certificate.IsActive)
                .OrderByDescending(certificate => certificate.IssueDate)
                .ToListAsync();

            return values.Count == 0 ? Content(string.Empty) : View(values);
        }
    }
}
