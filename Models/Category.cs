using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

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

        // --- KULLANICIYA ÖZEL ALAN ---
        // KANKA: Null ise herkes görür (Sistem Kategorisi), değer varsa sadece o User görür.
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // --- HİYERARŞİ İÇİN KRİTİK ALANLAR ---
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        public virtual ICollection<Category>? SubCategories { get; set; } = new List<Category>();

        public int Level { get; set; } = 1;
    }
}