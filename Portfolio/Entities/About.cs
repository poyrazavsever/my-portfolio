using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class About
    {
        public int AboutId { get; set; }

        [Display(Name = "Ad Soyad")]
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla {1} karakter olabilir.")]
        public string NameSurname { get; set; } = string.Empty;

        [Display(Name = "Konum")]
        [StringLength(100, ErrorMessage = "Konum en fazla {1} karakter olabilir.")]
        public string? Location { get; set; }

        [Display(Name = "Uzmanlık")]
        [StringLength(100, ErrorMessage = "Uzmanlık en fazla {1} karakter olabilir.")]
        public string? Expert { get; set; }

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "Hakkımda Metni")]
        [StringLength(2000, ErrorMessage = "Metin en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Yeni işlere açığım")]
        public bool OpenToWork { get; set; }
    }
}
