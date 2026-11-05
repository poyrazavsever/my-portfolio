using Portfolio.Entities;

namespace Portfolio.Areas.Admin.Models
{
    public class MessageListViewModel
    {
        public List<Message> Messages { get; init; } = [];

        /// <summary>Gönderen adı, e-posta veya konuda aranan metin.</summary>
        public string? Query { get; init; }

        /// <summary>"all" | "unread" | "read"</summary>
        public string Status { get; init; } = "all";

        /// <summary>"newest" | "oldest"</summary>
        public string Sort { get; init; } = "newest";

        /// <summary>Filtreden bağımsız toplam ve okunmamış sayıları (özet şeridi için).</summary>
        public int TotalCount { get; init; }

        public int UnreadCount { get; init; }

        /// <summary>Dashboard'dan "?open=12" ile gelindiyse bu mesajın modalı sayfa açılınca açılır.</summary>
        public int? OpenId { get; init; }

        public bool IsFiltered => !string.IsNullOrWhiteSpace(Query) || Status != "all";
    }
}
