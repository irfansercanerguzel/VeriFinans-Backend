using System;

namespace VeriFinans.Dtos
{
    public class ExpenseDto
    {
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public int? CreditCardId { get; set; }
        public int InstallmentCount { get; set; } = 1;
        public bool IsRecurring { get; set; } = false;
        public DateTime Date { get; set; }
    }

    public class IncomeDto
    {
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public bool IsRecurring { get; set; } = false;
        public DateTime Date { get; set; }
    }
}