using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeriFinans.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Color { get; set; }
        public string? Icon { get; set; }

        // 0: Gelir (Income), 1: Gider (Expense)
        [Required]
        public int Type { get; set; }

        // --- HİYERARŞİ İÇİN KRİTİK ALANLAR ---

        // Üst kategorinin Id'si (Eğer boşsa bu bir ANA kategoridir: Örn: Araba)
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        // Bu kategorinin altındaki evlatlar (Örn: FR 2104, KC 105)
        public virtual ICollection<Category>? SubCategories { get; set; } = new List<Category>();

        // Seviye takibi (Opsiyonel ama raporlama için hayat kurtarır: 1, 2, 3)
        public int Level { get; set; } = 1;
    }
}