namespace Portfolio.Areas.Admin.Models
{
    /// <summary>Sidebar'daki tek bir menü satırı. IsSeparator true ise sadece ayırıcı çizgi çizilir.</summary>
    public record AdminNavItem(
        string Title,
        string Icon,
        string? Controller = null,
        string? Action = null,
        bool IsReady = true,
        int Badge = 0,
        string? ExternalUrl = null,
        bool IsSeparator = false)
    {
        public static AdminNavItem Separator() => new(string.Empty, string.Empty, IsSeparator: true);
    }

    public class AdminSidebarViewModel
    {
        public List<AdminNavItem> Items { get; init; } = [];

        public string? ActiveController { get; init; }

        public bool IsMobile { get; init; }
    }
}
