using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Certificate
    {
        public int CertificateId { get; set; }

        [Display(Name = "Sertifika")]
        [Required(ErrorMessage = "Sertifika adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Sertifika adı en fazla {1} karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Veren kurum")]
        [Required(ErrorMessage = "Veren kurum zorunludur.")]
        [StringLength(100, ErrorMessage = "Kurum adı en fazla {1} karakter olabilir.")]
        public string Issuer { get; set; } = string.Empty;

        [Display(Name = "Alınma tarihi")]
        [Required(ErrorMessage = "Alınma tarihi zorunludur.")]
        public DateOnly IssueDate { get; set; }

        [Display(Name = "Doğrulama bağlantısı")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [StringLength(500)]
        public string? CredentialUrl { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}
