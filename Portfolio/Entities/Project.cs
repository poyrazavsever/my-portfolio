using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Project
    {
        public int ProjectId { get; set; }

        [Display(Name = "Proje Adı")]
        [Required(ErrorMessage = "Proje adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Proje adı en fazla {1} karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Proje bağlantısı")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [StringLength(500)]
        public string? GithubUrl { get; set; }

        [Display(Name = "Kapak Görseli")]
        [StringLength(500)]
        public string? CoverImageUrl { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Kategori")]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçiniz.")]
        public int CategoryId { get; set; }

        // Nullable: formdan sadece CategoryId gelir, nesnenin kendisi gelmez.
        // Non-nullable olsaydı MVC bunu otomatik "zorunlu" sayıp formu geçersiz kılardı.
        public Category? Category { get; set; }
    }
}
