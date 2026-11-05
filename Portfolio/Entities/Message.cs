using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Message
    {
        public int MessageId { get; set; }

        [Display(Name = "Ad Soyad")]
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla {1} karakter olabilir.")]
        public string NameSurname { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150, ErrorMessage = "E-posta en fazla {1} karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Konu")]
        [Required(ErrorMessage = "Konu zorunludur.")]
        [StringLength(150, ErrorMessage = "Konu en fazla {1} karakter olabilir.")]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Mesaj")]
        [Required(ErrorMessage = "Mesaj zorunludur.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mesaj {2} ile {1} karakter arasında olmalıdır.")]
        public string MessageDetail { get; set; } = string.Empty;

        public DateTime SendDate { get; set; }

        public bool IsRead { get; set; }
    }
}
