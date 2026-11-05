using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Experience
    {
        public int ExperienceId { get; set; }

        [Display(Name = "Pozisyon")]
        [Required(ErrorMessage = "Pozisyon zorunludur.")]
        [StringLength(100, ErrorMessage = "Pozisyon en fazla {1} karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Şirket")]
        [Required(ErrorMessage = "Şirket adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Şirket adı en fazla {1} karakter olabilir.")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Alt Başlık")]
        [StringLength(100, ErrorMessage = "Alt başlık en fazla {1} karakter olabilir.")]
        public string? SideTitle { get; set; }

        [Display(Name = "Çalışma Türü")]
        [StringLength(50, ErrorMessage = "Çalışma türü en fazla {1} karakter olabilir.")]
        public string? WorkType { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        public DateOnly StartDate { get; set; }

        // null = hâlâ devam ediyor
        [Display(Name = "Bitiş Tarihi")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
    }
}
