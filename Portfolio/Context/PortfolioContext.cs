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

        public DbSet<Category> Categories { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<Skill> Skills { get; set; }

        public DbSet<SiteSetting> SiteSettings { get; set; }

        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İçinde proje olan bir kategori silinemesin: projeler sessizce kaybolmasın.
            // Data Annotation ile ifade edilemeyen kurallar Fluent API ile yazılır.
            modelBuilder.Entity<Project>()
                .HasOne(project => project.Category)
                .WithMany(category => category.Projects)
                .HasForeignKey(project => project.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
