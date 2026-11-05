using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Contact
    {
        public int ContactId { get; set; }

        [Display(Name = "Başlık Metni")]
        [StringLength(300, ErrorMessage = "Metin en fazla {1} karakter olabilir.")]
        public string? Description { get; set; }

        [Display(Name = "Alt Metin")]
        [StringLength(300, ErrorMessage = "Metin en fazla {1} karakter olabilir.")]
        public string? SubDescription { get; set; }

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "Konum")]
        [StringLength(100)]
        public string? Location { get; set; }

        [Display(Name = "LinkedIn")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [StringLength(500)]
        public string? Linkedin { get; set; }

        [Display(Name = "GitHub")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [StringLength(500)]
        public string? Github { get; set; }
    }
}
