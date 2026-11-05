using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Models;

namespace Portfolio.Infrastructure
{
    /// <summary>Birden fazla View Component'in ihtiyaç duyduğu ortak sorgular.</summary>
    public static class SiteQueries
    {
        public static async Task<SiteChromeViewModel> LoadChromeAsync(PortfolioContext context)
        {
            var name = await context.HeroSections.AsNoTracking().Select(hero => hero.Title).FirstOrDefaultAsync();
            var contact = await context.Contacts.AsNoTracking().FirstOrDefaultAsync();
            var hasCv = await context.SiteSettings.AsNoTracking().AnyAsync(setting => setting.CvFilePath != null);

            return new SiteChromeViewModel
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Poyraz Avsever" : name,
                Email = contact?.Email,
                Linkedin = contact?.Linkedin,
                Github = contact?.Github,
                HasCv = hasCv,
            };
        }
    }
}
