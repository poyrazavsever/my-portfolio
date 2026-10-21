using Microsoft.EntityFrameworkCore;
using Portfolio.Entities;

namespace Portfolio.Context
{
    public class PortfolioContext : DbContext
    {
        public PortfolioContext(DbContextOptions<PortfolioContext> options)
            : base(options)
        {
        }

        public DbSet<About> Abouts { get; set; }

        public DbSet<Education> Educations { get; set; }

        public DbSet<Experience> Experiences { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<HeroSection> HeroSections { get; set; }

    }
}
