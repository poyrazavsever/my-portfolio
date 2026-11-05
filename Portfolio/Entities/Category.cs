using System.ComponentModel.DataAnnotations;

namespace Portfolio.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Display(Name = "Kategori Adı")]
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Kategori adı en fazla {1} karakter olabilir.")]
        public string CategoryName { get; set; } = string.Empty;

        public List<Project> Projects { get; set; } = [];
    }
}
