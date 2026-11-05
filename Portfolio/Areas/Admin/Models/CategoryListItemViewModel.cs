namespace Portfolio.Areas.Admin.Models
{
    /// <summary>Kategori listesinde satır başına gereken veri: projelerin kendisi değil, sadece sayısı.</summary>
    public class CategoryListItemViewModel
    {
        public int CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public int ProjectCount { get; init; }

        public int ActiveProjectCount { get; init; }
    }
}
