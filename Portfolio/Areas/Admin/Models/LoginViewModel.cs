using System.ComponentModel.DataAnnotations;

namespace Portfolio.Areas.Admin.Models
{
    public class LoginViewModel
    {
        [Display(Name = "Kullanıcı adı")]
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Şifre")]
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [StringLength(200)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
