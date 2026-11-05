using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class HeroSection
    {
        public int HeroSectionId { get; set; }

        [Display(Name = "Ad Soyad")]
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla {1} karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Unvan")]
        [StringLength(150, ErrorMessage = "Unvan en fazla {1} karakter olabilir.")]
        public string? SubTitle { get; set; }

        [Display(Name = "Tanıtım Metni")]
        [StringLength(500, ErrorMessage = "Tanıtım metni en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Profil Görseli")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
