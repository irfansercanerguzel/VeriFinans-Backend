using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeriFinans.Models
{
    public class Income
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
   

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public bool IsRecurring { get; set; }
    }
}