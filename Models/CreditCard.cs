using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeriFinans.Models
{
    public class CreditCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string CardName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Limit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentDebt { get; set; } = 0;

        [Required]
        public int ClosingDay { get; set; }

        public DateTime GetDueDate(int year, int month)
        {
            if (year < 1 || year > 9999) year = DateTime.Now.Year;
            if (month < 1 || month > 12) month = DateTime.Now.Month;

            int daysInMonth = DateTime.DaysInMonth(year, month);
            int safeDay = Math.Clamp(this.ClosingDay, 1, daysInMonth);

            DateTime closingDate = new DateTime(year, month, safeDay);
            DateTime dueDate = closingDate.AddDays(10);

            if (dueDate.DayOfWeek == DayOfWeek.Saturday)
            {
                dueDate = dueDate.AddDays(2);
            }
            else if (dueDate.DayOfWeek == DayOfWeek.Sunday)
            {
                dueDate = dueDate.AddDays(1);
            }

            return dueDate;
        }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<Expense>? Expenses { get; set; }
    }
}