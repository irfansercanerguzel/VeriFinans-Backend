using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VeriFinans.Data;
using VeriFinans.Models;

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            // ===============================================================
            // 1. KUSURSUZ GENEL FİNANSAL DURUM (Kuruş Hassasiyetli & Performanslı)
            // ===============================================================
            var totalIncomeAllTime = await _context.Incomes
                .Where(i => i.UserId == userId)
                .SumAsync(i => i.Amount);

            // NAKİT ÇIKIŞLAR: Cüzdandan çıkan net nakit
            var totalCashExpenseAllTime = await _context.Expenses
                .Where(e => e.UserId == userId && e.CreditCardId == null)
                .SumAsync(e => e.Amount);

            // ÖDENMEMİŞ KART BORÇLARI: 
            // KANKA: 0.02 TL altındaki "hayalet" kuruşları borç saymaması için süzüyoruz.
            var unpaidAmounts = await _context.Expenses
                .Where(e => e.UserId == userId && e.CreditCardId != null && e.IsPaid == false)
                .Select(e => e.Amount)
                .ToListAsync();

            decimal totalUnpaidCardExpenses = unpaidAmounts.Where(a => a > 0.02m).Sum();

            // Toplam Gider = Nakit Harcamalar + Ödenmemiş Kart Borçları
            decimal totalExpenseAllTime = totalCashExpenseAllTime + totalUnpaidCardExpenses;

            // Güncel Nakit Bakiyesi (Gelir - Nakit Çıkışı)
            decimal currentBalance = totalIncomeAllTime - totalCashExpenseAllTime;

            // Net Akış (Tasarruf/Açık Durumu)
            decimal netFlow = totalIncomeAllTime - totalExpenseAllTime;

            // ===============================================================
            // 2. KARTLAR VE DİNAMİK EKSTRE/TAKSİT MANTIĞI
            // ===============================================================
            var cards = await _context.CreditCards.AsNoTracking().Where(c => c.UserId == userId).ToListAsync();
            var cardDetails = new List<object>();
            var pendingDetailsList = new List<object>();

            decimal totalStatementDebts = 0;
            decimal totalActualDebt = 0;
            decimal totalPendingExpenses = 0;

            foreach (var card in cards)
            {
                int safeDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(now.Year, now.Month));
                DateTime thisMonthClosingDate = new DateTime(now.Year, now.Month, safeDay, 23, 59, 59, DateTimeKind.Utc);

                DateTime lastStatementDate;
                DateTime nextStatementDate;

                if (now >= thisMonthClosingDate)
                {
                    lastStatementDate = thisMonthClosingDate;
                    var nextMonth = now.AddMonths(1);
                    nextStatementDate = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(card.ClosingDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)), 23, 59, 59, DateTimeKind.Utc);
                }
                else
                {
                    nextStatementDate = thisMonthClosingDate;
                    var prevMonth = now.AddMonths(-1);
                    lastStatementDate = new DateTime(prevMonth.Year, prevMonth.Month, Math.Min(card.ClosingDay, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month)), 23, 59, 59, DateTimeKind.Utc);
                }

                // KANKA: Her kart için DB'ye 3-4 kere gitmek yerine, o kartın ödenmemişlerini bir kerede çekip hafızada hesaplıyoruz.
                var cardExpenses = await _context.Expenses
                    .Where(e => e.CreditCardId == card.Id && e.IsPaid == false)
                    .Select(e => new { e.Date, e.Amount })
                    .ToListAsync();

                // 0.02m filtresiyle banka taksit bölmelerinden kalan 1 kuruşluk "çöpleri" borç göstermiyoruz.
                decimal statementDebt = cardExpenses.Where(e => e.Date <= lastStatementDate && e.Amount > 0.02m).Sum(e => e.Amount);
                decimal newPeriodDebt = cardExpenses.Where(e => e.Date > lastStatementDate && e.Date <= nextStatementDate && e.Amount > 0.02m).Sum(e => e.Amount);
                decimal futureDebt = cardExpenses.Where(e => e.Date > nextStatementDate && e.Amount > 0.02m).Sum(e => e.Amount);

                decimal allPendingDebt = newPeriodDebt + futureDebt;
                decimal currentDebt = statementDebt + newPeriodDebt;

                totalStatementDebts += statementDebt;
                totalActualDebt += currentDebt;

                cardDetails.Add(new
                {
                    id = card.Id,
                    cardName = card.CardName,
                    currentDebt = currentDebt,
                    statementDebt = statementDebt,
                    newPeriodDebt = newPeriodDebt,
                    closingDay = card.ClosingDay,
                    dueDay = card.GetDueDate(now.Year, now.Month).Day
                });

                if (allPendingDebt > 0.02m)
                {
                    pendingDetailsList.Add(new
                    {
                        cardName = card.CardName,
                        amount = allPendingDebt
                    });
                    totalPendingExpenses += allPendingDebt;
                }
            }

            // ===============================================================
            // 3. GRAFİKLER İÇİN TÜM ZAMANLAR (Ödemeler Çift Görünmesin Diye Filtreli)
            // ===============================================================
            var allTimeChartExpenses = await _context.Expenses
                .Where(e => e.UserId == userId && !e.Description.Contains("Kart Ödemesi"))
                .Include(e => e.Category)
                .ToListAsync();

            var chartData = allTimeChartExpenses
                .GroupBy(e => e.Category?.Name ?? "Diğer")
                .Select(g => new {
                    name = g.Key,
                    value = g.Sum(x => x.Amount),
                    color = g.FirstOrDefault()?.Category?.Color ?? "#3b82f6"
                }).OrderByDescending(x => x.value).ToList();

            var allTimeIncomes = await _context.Incomes
                .Where(i => i.UserId == userId)
                .Include(i => i.Category)
                .ToListAsync();

            var incomeChartData = allTimeIncomes
                .GroupBy(i => i.Category?.Name ?? "Diğer")
                .Select(g => new {
                    name = g.Key,
                    value = g.Sum(x => x.Amount)
                }).OrderByDescending(x => x.value).ToList();

            // ===============================================================
            // 4. BİLDİRİMLER
            // ===============================================================
            var alerts = new List<string>();
            if (currentBalance < 1500) alerts.Add("📢 Nakit bakiyen kritik seviyede (1.500₺ altı).");
            foreach (var card in cards)
            {
                var dueDate = card.GetDueDate(currentYear, currentMonth);
                int daysUntilDue = (dueDate.Date - now.Date).Days;
                if (daysUntilDue >= 0 && daysUntilDue <= 5)
                    alerts.Add($"💳 {card.CardName} ödemesi için son {daysUntilDue} gün!");
            }

            // ===============================================================
            // 5. RESPONSE
            // ===============================================================
            return Ok(new
            {
                stats = new
                {
                    currentBalance = currentBalance,
                    totalDebt = totalActualDebt,
                    monthlyTotal = totalStatementDebts,
                    pendingExpenses = totalPendingExpenses,
                    pendingDetails = pendingDetailsList,
                    totalIncomeAllTime = totalIncomeAllTime,
                    totalExpenseAllTime = totalExpenseAllTime,
                    netFlow = netFlow
                },
                cards = cardDetails,
                chartData = chartData,
                incomeChartData = incomeChartData,
                notifications = alerts,
                userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Kullanıcı"
            });
        }
    }
}