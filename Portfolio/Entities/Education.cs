using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Education
    {
        public int EducationId { get; set; }

        [Display(Name = "Okul")]
        [Required(ErrorMessage = "Okul adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Okul adı en fazla {1} karakter olabilir.")]
        public string EducationTitle { get; set; } = string.Empty;

        [Display(Name = "Bölüm / Derece")]
        [StringLength(150, ErrorMessage = "Bölüm en fazla {1} karakter olabilir.")]
        public string? SubTitle { get; set; }

        [Display(Name = "Açıklama")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

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
