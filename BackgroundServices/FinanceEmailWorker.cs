namespace VeriFinans.BackgroundServices
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using VeriFinans.Data;
    using Microsoft.EntityFrameworkCore;
    using VeriFinans.Services;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class FinanceEmailWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        // KANKA: Aynı gün içinde 14:00'te peş peşe 50 tane mail atmasın diye
        // en son hangi tarihte mail atıldığını hafızada tutuyoruz.
        private DateTime _lastRunDate = DateTime.MinValue;

        public FinanceEmailWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // SAAT KONTROLÜ: Saat 14:00 veya sonrasındaysak VE bugün henüz çalışmadıysa işlemi başlat!
                // Neden >= 14? Çünkü sunucu tam 14:00'te kapalı kalırsa, 14:15'te açıldığında bugünü ıskalamasın.
                if (now.Hour >= 14 && _lastRunDate.Date != now.Date)
                {
                    await ProcessDailyTasksAsync();

                    // Bugünün işi bitti, yarına kadar bu if'e girmeyecek.
                    _lastRunDate = now.Date;
                }

                // Döngüyü 24 saat değil, sadece 1 dakika bekletip saati kontrol ediyoruz. Sistemi yormaz.
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        // MAİL VE VERİTABANI İŞLEMLERİNİN YAPILDIĞI ANA METOT
        private async Task ProcessDailyTasksAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var today = DateTime.Now.Date;
                var tomorrow = today.AddDays(1);

                // --- 1. OTOMATİK GELİR KAYDI (MAAŞ VB.) ---
                var wallets = await dbContext.Wallets
                    .Include(w => w.User)
                    .Where(w => w.AutoIncomeEnabled && w.IncomeDayOfMonth == today.Day)
                    .ToListAsync();

                foreach (var wallet in wallets)
                {
                    wallet.Balance += wallet.MonthlyIncomeAmount;

                    string subject = "💰 Bakiye Güncellemesi: Gelir Eklendi";
                    string body = $"<h3>Merhaba {wallet.User?.Name},</h3>" +
                                 $"<p>Tanımladığınız <b>{wallet.MonthlyIncomeAmount} TL</b> tutarındaki aylık geliriniz cüzdan bakiyenize eklenmiştir.</p>" +
                                 $"<p><b>Yeni Toplam Bakiyeniz:</b> {wallet.Balance} TL</p>";

                    await emailService.SendEmailAsync(wallet.User!.Email, subject, body);
                }

                // --- 2. TAKSİTLİ HARCAMA YÖNETİMİ ---
                var activeInstallments = await dbContext.Expenses
                    .Where(e => e.InstallmentCount > 1 &&
                                e.CurrentInstallment < e.InstallmentCount &&
                                e.RecurringDay == today.Day)
                    .ToListAsync();

                foreach (var installment in activeInstallments)
                {
                    installment.CurrentInstallment++;

                    if (installment.CreditCardId != null)
                    {
                        var card = await dbContext.CreditCards.FindAsync(installment.CreditCardId);
                        if (card != null)
                        {
                            decimal monthlyAmount = installment.Amount / installment.InstallmentCount;
                            card.CurrentDebt += monthlyAmount;
                        }
                    }
                }

                // --- 3. DÜZENLİ GİDER KAYDI (AİDAT, ABONELİK VB.) ---
                var recurringExpenses = await dbContext.Expenses
                    .Include(e => e.User)
                    .Where(e => e.IsRecurring && e.RecurringDay == today.Day)
                    .ToListAsync();

                foreach (var expense in recurringExpenses)
                {
                    var userWallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == expense.UserId);
                    if (userWallet != null)
                    {
                        userWallet.Balance -= expense.Amount;

                        string subject = "📊 Cüzdan Bilgisi: Düzenli Gider İşlendi";
                        string body = $"<h3>Harcama Kaydı Bilgilendirmesi</h3>" +
                                     $"<p>Her ay otomatik olarak işlenen <b>{expense.Description}</b> tutarı (<b>{expense.Amount} TL</b>) bakiyenizden düşülmüştür.</p>" +
                                     $"<p><b>Güncel Kalan Nakit:</b> {userWallet.Balance} TL</p>";

                        await emailService.SendEmailAsync(expense.User!.Email, subject, body);
                    }
                }

                // --- 4. DİNAMİK KREDİ KARTI HATIRLATMASI (YENİ SİSTEM) ---
                var allCards = await dbContext.CreditCards
                    .Include(c => c.User)
                    .ToListAsync();

                foreach (var card in allCards)
                {
                    DateTime calculatedDueDate = card.GetDueDate(today.Year, today.Month);

                    string isShiftedNote = calculatedDueDate.DayOfWeek == DayOfWeek.Monday ?
                        "<p style='color: #888;'><i>(Ödeme günü hafta sonuna geldiği için Pazartesiye kaydırılmıştır.)</i></p>" : "";

                    // DURUM 1: SON ÖDEME TARİHİNE 1 GÜN KALDIYSA (Yarın)
                    if (calculatedDueDate.Date == tomorrow.Date && card.CurrentDebt > 0)
                    {
                        string subject = $"🚨 Yaklaşan Ödeme: {card.CardName} (Son 1 Gün)";
                        string body = $@"
                            <div style='font-family: Arial, sans-serif; border: 1px solid #ddd; padding: 20px;'>
                                <h2 style='color: #f0ad4e;'>Son Ödeme Tarihine 1 Gün Kaldı!</h2>
                                <p><b>{card.CardName}</b> kredi kartınızın son ödeme günü YARIN (<b>{calculatedDueDate:dd/MM/yyyy}</b>).</p>
                                {isShiftedNote}
                                <p style='font-size: 18px;'><b>Güncel Kart Borcu:</b> <span style='color: #d9534f;'>{card.CurrentDebt} TL</span></p>
                                <hr>
                                <p>Gecikme faizi işlememesi için hesabınızı kontrol etmeyi unutmayınız.</p>
                            </div>";

                        await emailService.SendEmailAsync(card.User!.Email, subject, body);
                    }
                    // DURUM 2: SON ÖDEME TARİHİ BUGÜNSE (Bugün)
                    else if (calculatedDueDate.Date == today.Date && card.CurrentDebt > 0)
                    {
                        string subject = $"❗ DİKKAT: {card.CardName} Kartınızın Ödeme Günü BUGÜN!";
                        string body = $@"
                            <div style='font-family: Arial, sans-serif; border: 2px solid #d9534f; padding: 20px;'>
                                <h2 style='color: #d9534f;'>Bugün Son Ödeme Günü!</h2>
                                <p><b>{card.CardName}</b> kredi kartınızın son ödeme günü BUGÜN (<b>{calculatedDueDate:dd/MM/yyyy}</b>).</p>
                                {isShiftedNote}
                                <p style='font-size: 18px;'><b>Ödenmesi Gereken Tutar:</b> <span style='color: #d9534f;'><b>{card.CurrentDebt} TL</b></span></p>
                                <hr>
                                <p>Kredi notunuzun düşmemesi için bugün gün bitmeden ödemenizi bankanız üzerinden gerçekleştiriniz.</p>
                            </div>";

                        await emailService.SendEmailAsync(card.User!.Email, subject, body);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}