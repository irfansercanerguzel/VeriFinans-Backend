using System.ComponentModel.DataAnnotations;

namespace VeriFinans.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "user";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Expense> Expenses { get; set; }
        public ICollection<Income> Incomes { get; set; }
        public ICollection<CreditCard> CreditCards { get; set; }

        public ICollection<Category> Categories { get; set; }
    }
}