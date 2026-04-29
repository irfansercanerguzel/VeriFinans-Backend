namespace VeriFinans.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public decimal Balance { get; set; } = 0; // Anlık nakit bakiye
        public string Currency { get; set; } = "TRY";

        // Kullanıcı ile ilişki
        public int UserId { get; set; }
        public User User { get; set; }

        // Otomatik Gelir/Gider Ayarları
        public bool AutoIncomeEnabled { get; set; } = false; // Maaş her ay otomatik yatsın mı?
        public decimal MonthlyIncomeAmount { get; set; } = 0;
        public int IncomeDayOfMonth { get; set; } = 1; // Her ayın kaçında?
    }
}