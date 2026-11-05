using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Models;
using Portfolio.Services;

namespace Portfolio.Infrastructure
{
    /// <summary>Birden fazla View Component'in ihtiyaç duyduğu ortak sorgular.</summary>
    public static class SiteQueries
    {
        public static async Task<SiteChromeViewModel> LoadChromeAsync(PortfolioContext context, ICvStorage cvStorage)
        {
            var name = await context.HeroSections.AsNoTracking().Select(hero => hero.Title).FirstOrDefaultAsync();
            var contact = await context.Contacts.AsNoTracking().FirstOrDefaultAsync();

            return new SiteChromeViewModel
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Poyraz Avsever" : name,
                Email = contact?.Email,
                Linkedin = contact?.Linkedin,
                Github = contact?.Github,
                HasCv = await HasCvAsync(context, cvStorage),
            };
        }

        /// <summary>
        /// "CV indir" butonu yalnızca DB'de kayıt VE diskte dosya varsa görünür.
        /// (Repo yeni bir makinede kurulduğunda DB kaydı olup dosya olmayabilir; ölü bir buton göstermeyelim.)
        /// </summary>
        public static async Task<bool> HasCvAsync(PortfolioContext context, ICvStorage cvStorage)
        {
            var path = await context.SiteSettings.AsNoTracking().Select(setting => setting.CvFilePath).FirstOrDefaultAsync();
            return cvStorage.Exists(path);
        }
    }
}
