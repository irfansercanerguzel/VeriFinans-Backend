namespace VeriFinans.DTOs
{
    public class WalletStatusDto
    {
        public decimal CurrentBalance { get; set; } // Wallet.Balance (Nakit)
        public decimal TotalCreditCardDebt { get; set; } // Tüm kartların toplam borcu
        public decimal ProjectedBalance { get; set; } // CurrentBalance - TotalCreditCardDebt

        // Opsiyonel: Ay sonuna kadar bekleyen sabit giderler (Aidat vb.)
        public decimal PendingRecurringExpenses { get; set; }
    }
}
