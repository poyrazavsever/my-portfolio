using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    // Tek satırlık ayar tablosu: sitenin genel ayarları ve CV dosyasının yeri.
    public class SiteSetting
    {
        public int SiteSettingId { get; set; }

        [Display(Name = "Site Başlığı")]
        [StringLength(100)]
        public string? SiteTitle { get; set; }

        // wwwroot'a göre göreli yol, örn. "/uploads/cv/3f2a...pdf"
        [StringLength(500)]
        public string? CvFilePath { get; set; }

        // Ziyaretçinin indirdiğinde göreceği dosya adı, örn. "Poyraz-Avsever-CV.pdf"
        [StringLength(150)]
        public string? CvDownloadName { get; set; }

        public DateTime? CvUpdatedAt { get; set; }
    }
}
