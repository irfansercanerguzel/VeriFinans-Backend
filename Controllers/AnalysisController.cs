using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VeriFinans.Data;

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalysisController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedAnalysis(
            [FromQuery] int? periodType = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null,
            [FromQuery] int? accountId = null)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            DateTime now = DateTime.Now;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;

            // ==========================================
            // 1. ZAMAN VE KART FİLTRESİ ALGORİTMASI
            // ==========================================

            // A) EĞER KULLANICI COMBOBOX'TAN ÖZEL BİR AY SEÇTİYSE
            if (month.HasValue && year.HasValue)
            {
                if (accountId.HasValue && accountId.Value > 0)
                {
                    var card = await _context.CreditCards.FindAsync(accountId.Value);
                    if (card != null)
                    {
                        // Sadece seçilen ayın ekstresini getir
                        int safeDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(year.Value, month.Value));
                        endDate = new DateTime(year.Value, month.Value, safeDay, 23, 59, 59);

                        var prevM = endDate.AddMonths(-1);
                        int prevSafeDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(prevM.Year, prevM.Month));
                        startDate = new DateTime(prevM.Year, prevM.Month, prevSafeDay, 23, 59, 59).AddSeconds(1);
                    }
                }
                else
                {
                    // Kart yoksa normal takvim ayı (Örn: 1 Nisan - 30 Nisan)
                    startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0);
                    endDate = startDate.AddMonths(1).AddTicks(-1);
                }
            }
            // B) HİÇBİR FİLTRE YOKSA VEYA "TÜMÜ, 3 AY" GİBİ BUTONLARA BASILDIYSA (VARSAYILAN: AKTİF DÖNEM)
            else
            {
                if (accountId.HasValue && accountId.Value > 0)
                {
                    var card = await _context.CreditCards.FindAsync(accountId.Value);
                    if (card != null)
                    {
                        // KANKA BAKIYORUZ: Tam senin istediğin Aktif Dönem Hesaplaması
                        int safeDayThisMonth = Math.Min(card.ClosingDay, DateTime.DaysInMonth(now.Year, now.Month));
                        DateTime currentMonthClosing = new DateTime(now.Year, now.Month, safeDayThisMonth, 23, 59, 59);

                        // Eğer bugün, bu ayın hesap kesimini GEÇTİYSEK (Yani 29 Nisan > 18 Nisan)
                        if (now > currentMonthClosing)
                        {
                            // AKTİF DÖNEM: 19 Nisan - 18 Mayıs
                            startDate = currentMonthClosing.AddSeconds(1);
                            DateTime nextM = now.AddMonths(1);
                            int safeDayNextMonth = Math.Min(card.ClosingDay, DateTime.DaysInMonth(nextM.Year, nextM.Month));
                            endDate = new DateTime(nextM.Year, nextM.Month, safeDayNextMonth, 23, 59, 59);
                        }
                        // Eğer bugün, bu ayın hesap kesimi HENÜZ GELMEDİYSEK (Yani 10 Nisan < 18 Nisan)
                        else
                        {
                            // AKTİF DÖNEM: 19 Mart - 18 Nisan
                            endDate = currentMonthClosing;
                            DateTime prevM = now.AddMonths(-1);
                            int safeDayPrevMonth = Math.Min(card.ClosingDay, DateTime.DaysInMonth(prevM.Year, prevM.Month));
                            startDate = new DateTime(prevM.Year, prevM.Month, safeDayPrevMonth, 23, 59, 59).AddSeconds(1);
                        }

                        // Eğer 3 Ay, 6 Ay periyodu seçildiyse, sadece başlangıcı geriye çekiyoruz, bitiş (Aktif Ekstre sonu) sabit kalıyor.
                        if (periodType.HasValue && periodType.Value != 999)
                        {
                            startDate = endDate.AddMonths(-periodType.Value).AddSeconds(1);
                        }
                        else if (periodType == 999)
                        {
                            startDate = DateTime.MinValue;
                            endDate = now.AddMonths(1);
                        }
                    }
                }
                else
                {
                    // Kart Yoksa (Genel Nakit Özeti)
                    if (periodType == 999)
                    {
                        startDate = DateTime.MinValue;
                        endDate = now.AddMonths(1);
                    }
                    else if (periodType.HasValue)
                    {
                        endDate = now;
                        startDate = now.AddMonths(-periodType.Value);
                    }
                    else
                    {
                        // Varsayılan: Bulunduğumuz ayın 1'i ile sonu
                        startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                        endDate = startDate.AddMonths(1).AddTicks(-1);
                    }
                }
            }

            // ==========================================
            // 2. VERİLERİ ÇEK VE HİYERARŞİYİ KUR
            // ==========================================
            var allCategories = await _context.Categories.AsNoTracking().ToListAsync();

            var query = _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate && !e.Description.Contains("Kart Ödemesi"));

            if (accountId == 0) query = query.Where(e => e.CreditCardId == null);
            else if (accountId > 0) query = query.Where(e => e.CreditCardId == accountId);

            var expenses = await query.ToListAsync();

            decimal totalExpense = expenses.Sum(e => e.Amount);
            int days = Math.Max((endDate - startDate).Days, 1);
            decimal dailyAverage = periodType == 999 ? 0 : Math.Floor(totalExpense / days);

            var enrichedExpenses = expenses.Select(e => {
                var cat = allCategories.FirstOrDefault(c => c.Id == e.CategoryId);
                string l1 = "Diğer";
                string l2 = "Genel";
                string l3 = string.IsNullOrWhiteSpace(e.Description) ? "İşlem" : e.Description;

                if (cat != null)
                {
                    if (cat.Level == 3)
                    {
                        l3 = cat.Name;
                        var parentL2 = allCategories.FirstOrDefault(c => c.Id == cat.ParentId);
                        if (parentL2 != null)
                        {
                            l2 = parentL2.Name;
                            var parentL1 = allCategories.FirstOrDefault(c => c.Id == parentL2.ParentId);
                            if (parentL1 != null) l1 = parentL1.Name;
                        }
                    }
                    else if (cat.Level == 2)
                    {
                        l2 = cat.Name;
                        var parentL1 = allCategories.FirstOrDefault(c => c.Id == cat.ParentId);
                        if (parentL1 != null) l1 = parentL1.Name;
                    }
                    else if (cat.Level == 1)
                    {
                        l1 = cat.Name;
                    }
                }
                return new { Amount = e.Amount, L1 = l1, L2 = l2, L3 = l3, Date = e.Date };
            }).ToList();

            var categoryBreakdown = new List<object>();
            var l1Groups = enrichedExpenses.GroupBy(x => x.L1).ToList();

            string topCategoryName = "Veri Yok";
            decimal maxCatAmount = 0;

            foreach (var g1 in l1Groups)
            {
                decimal catTotal = g1.Sum(x => x.Amount);
                if (catTotal > maxCatAmount) { maxCatAmount = catTotal; topCategoryName = g1.Key; }

                var l2Groups = g1.GroupBy(x => x.L2).Select(g2 => new {
                    subCategoryName = g2.Key,
                    totalAmount = g2.Sum(x => x.Amount),
                    items = g2.Select(x => new {
                        detail = x.L3,
                        amount = x.Amount,
                        date = x.Date.ToString("dd MMM yyyy")
                    }).OrderByDescending(x => x.amount).ToList()
                }).OrderByDescending(x => x.totalAmount).ToList();

                categoryBreakdown.Add(new
                {
                    categoryName = g1.Key,
                    totalAmount = catTotal,
                    percentage = totalExpense > 0 ? Math.Round((catTotal / totalExpense) * 100, 1) : 0,
                    subGroups = l2Groups
                });
            }

            string periodText = periodType == 999 ? "Tüm Zamanlar" :
                                (!month.HasValue && !periodType.HasValue && accountId > 0) ? $"Aktif Ekstre: {startDate:dd MMM} - {endDate:dd MMM}" :
                                $"{startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";

            return Ok(new
            {
                totalExpense,
                dailyAverage,
                topCategoryName,
                periodText = periodText,
                categoryBreakdown = categoryBreakdown.OrderByDescending(c => (decimal)((dynamic)c).totalAmount).ToList()
            });
        }
    }
}