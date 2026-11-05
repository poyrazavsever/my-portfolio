using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Models
{
    /// <summary>
    /// Dashboard'un ihtiyaç duyduğu her şey tek bir modelde: sayılar, son kayıt listeleri, kategori özeti.
    /// View yalnızca bu modeli çizer; hiçbir sorgu view içinde yapılmaz.
    /// </summary>
    public class DashboardViewModel
    {
        // İstatistik kartları
        public int ProjectCount { get; init; }
        public int ActiveProjectCount { get; init; }
        public int SkillCount { get; init; }
        public int ExperienceCount { get; init; }
        public int EducationCount { get; init; }
        public int MessageCount { get; init; }
        public int UnreadMessageCount { get; init; }

        // Son kayıtlar
        public List<Message> LatestMessages { get; init; } = [];
        public List<Project> LatestProjects { get; init; } = [];

        // Kategori özeti (GroupBy sonucu)
        public List<CategorySummaryItem> CategorySummary { get; init; } = [];

        public bool HasCv { get; init; }
    }

    public class CategorySummaryItem
    {
        public string CategoryName { get; init; } = string.Empty;
        public int ProjectCount { get; init; }
        public int ActiveCount { get; init; }
    }
}
