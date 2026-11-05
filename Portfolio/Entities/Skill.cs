using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Skill
    {
        public int SkillId { get; set; }

        [Display(Name = "Yetenek")]
        [Required(ErrorMessage = "Yetenek adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Yetenek adı en fazla {1} karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Seviye (%)")]
        [Range(0, 100, ErrorMessage = "Seviye {1} ile {2} arasında olmalıdır.")]
        public int Level { get; set; }

        [Display(Name = "Sıra")]
        [Range(0, 999, ErrorMessage = "Sıra {1} ile {2} arasında olmalıdır.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}
