using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeriFinans.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] 
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(500)] 
        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? CreditCardId { get; set; }

        public int InstallmentCount { get; set; } = 1;

        public int CurrentInstallment { get; set; } = 1;

        public bool IsRecurring { get; set; } = false; 
        public int RecurringDay { get; set; } = 1;

        public bool IsPaid { get; set; } = false; // Varsayılan olarak ödenmedi

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("CreditCardId")]
        public CreditCard? CreditCard { get; set; }
    }
}