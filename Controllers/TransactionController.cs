using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VeriFinans.Data;
using VeriFinans.Models;
using VeriFinans.Dtos;
using VeriFinans.Services;

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AiService _aiService;

        public TransactionController(ApplicationDbContext context, AiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        // --- YARDIMCI METOD ---
        private int GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // --- 1. GİDER (HARCAMA) EKLEME (Kuruş Tamamlamalı En Güncel Sürüm) ---
        [HttpPost("expense")]
        public async Task<IActionResult> AddExpense([FromBody] ExpenseDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // KANKA: Kuruş kaybını önleyen algoritma burada başlıyor
                    // Örn: 1000 / 3 işleminde base 333.33 çıkar.
                    decimal baseInstallment = Math.Floor((dto.Amount / dto.InstallmentCount) * 100) / 100;

                    // 333.33 * 3 = 999.99 eder.
                    decimal totalDistributed = baseInstallment * dto.InstallmentCount;

                    // 1000 - 999.99 = 0.01 kuruş farkı buluyoruz.
                    decimal pennyDifference = dto.Amount - totalDistributed;

                    for (int i = 0; i < dto.InstallmentCount; i++)
                    {
                        decimal finalAmount = baseInstallment;

                        // EĞER SON TAKSİTSE: Havada kalan o kuruşu (pennyDifference) buraya ekle
                        if (i == dto.InstallmentCount - 1)
                        {
                            finalAmount += pennyDifference;
                        }

                        var expense = new Expense
                        {
                            Amount = finalAmount,
                            Description = dto.InstallmentCount > 1
                                ? $"{dto.Description} ({i + 1}/{dto.InstallmentCount})"
                                : dto.Description,
                            CategoryId = dto.CategoryId,
                            CreditCardId = dto.CreditCardId,
                            UserId = userId,
                            // Taksitleri tıkır tıkır gelecek aylara dağıtıyoruz
                            Date = dto.Date != default ? dto.Date.AddMonths(i).ToUniversalTime() : DateTime.UtcNow.AddMonths(i),
                            InstallmentCount = dto.InstallmentCount,
                            CurrentInstallment = i + 1,
                            IsRecurring = dto.IsRecurring
                        };
                        _context.Expenses.Add(expense);
                    }

                    if (dto.CreditCardId.HasValue)
                    {
                        var card = await _context.CreditCards.FindAsync(dto.CreditCardId);
                        if (card != null)
                        {
                            // Kart borcuna (limit kullanımına) harcamanın TAMAMINI tek seferde yansıtıyoruz
                            card.CurrentDebt += dto.Amount;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Harcama başarıyla eklendi." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Gider eklenirken hata oluştu.", error = ex.Message });
                }
            });
        }

        // --- 2. GELİR EKLEME ---
        [HttpPost("income")]
        public async Task<IActionResult> AddIncome([FromBody] IncomeDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var income = new Income
                    {
                        Amount = dto.Amount,
                        Description = dto.Description ?? "Gelir Kaydı",
                        CategoryId = dto.CategoryId,
                        UserId = userId,
                        Date = dto.Date != default ? dto.Date.ToUniversalTime() : DateTime.UtcNow,
                        IsRecurring = dto.IsRecurring
                    };

                    _context.Incomes.Add(income);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Gelir başarıyla işlendi." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Gelir kaydedilirken hata oluştu.", error = ex.Message });
                }
            });
        }

        // --- 3. GİDER DÜZENLEME (Kart Borcu Düzeltmeli) ---
        [HttpPut("expense/{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense == null) return NotFound();

            if (expense.CreditCardId.HasValue)
            {
                var card = await _context.CreditCards.FindAsync(expense.CreditCardId);
                if (card != null)
                {
                    card.CurrentDebt = (card.CurrentDebt - expense.Amount) + dto.Amount;
                }
            }

            expense.Amount = dto.Amount;
            expense.CategoryId = dto.CategoryId;
            expense.Description = dto.Description;
            expense.IsRecurring = dto.IsRecurring;
            if (dto.Date != default) expense.Date = dto.Date.ToUniversalTime();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gider güncellendi." });
        }

        // --- 4. GELİR DÜZENLEME ---
        [HttpPut("income/{id}")]
        public async Task<IActionResult> UpdateIncome(int id, [FromBody] IncomeDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
            if (income == null) return NotFound();

            income.Amount = dto.Amount;
            income.Description = dto.Description;
            income.CategoryId = dto.CategoryId;
            income.IsRecurring = dto.IsRecurring;
            if (dto.Date != default) income.Date = dto.Date.ToUniversalTime();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gelir güncellendi." });
        }

        // --- 5. SİLME (Kart Borcu İadeli) ---
        [HttpDelete("{type}/{id}")]
        public async Task<IActionResult> DeleteTransaction(string type, int id)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            if (type.ToLower() == "income")
            {
                var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
                if (income == null) return NotFound();
                _context.Incomes.Remove(income);
            }
            else
            {
                var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
                if (expense == null) return NotFound();

                if (expense.CreditCardId.HasValue)
                {
                    var card = await _context.CreditCards.FindAsync(expense.CreditCardId);
                    if (card != null)
                    {
                        card.CurrentDebt -= expense.Amount;
                        if (card.CurrentDebt < 0) card.CurrentDebt = 0;
                    }
                }

                _context.Expenses.Remove(expense);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "İşlem başarıyla silindi." });
        }

        // --- 6. SON İŞLEMLER LİSTESİ ---
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentTransactions()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .Take(10)
                .Select(e => new
                {
                    id = e.Id,
                    type = "expense",
                    amount = e.Amount,
                    category = e.Category != null ? e.Category.Name : "Genel",
                    date = e.Date,
                    description = e.Description,
                    isRecurring = e.IsRecurring
                })
                .ToListAsync();

            return Ok(expenses);
        }

        // --- 7. RECURRING LİSTELERİ ---
        [HttpGet("recurring-incomes")]
        public async Task<IActionResult> GetRecurringIncomes()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            return Ok(await _context.Incomes
                .Where(i => i.UserId == userId && i.IsRecurring)
                .Select(i => new { id = i.Id, type = "income", amount = i.Amount, description = i.Description, isRecurring = i.IsRecurring })
                .ToListAsync());
        }

        [HttpGet("recurring-expenses")]
        public async Task<IActionResult> GetRecurringExpenses()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            return Ok(await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.IsRecurring)
                .Select(e => new { id = e.Id, type = "expense", amount = e.Amount, description = e.Description, category = e.Category != null ? e.Category.Name : "Genel", isRecurring = e.IsRecurring })
                .ToListAsync());
        }

        // --- 8. AYLIK GELİR RAPORU ---
        [HttpGet("income-report")]
        public async Task<IActionResult> GetIncomeReport([FromQuery] int month, [FromQuery] int? year)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            int targetYear = year ?? DateTime.UtcNow.Year;
            var start = new DateTime(targetYear, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddTicks(-1);

            var incomes = await _context.Incomes
                .AsNoTracking()
                .Include(i => i.Category)
                .Where(i => i.UserId == userId && i.Date >= start && i.Date <= end)
                .OrderByDescending(i => i.Date)
                .Select(i => new
                {
                    id = i.Id,
                    amount = i.Amount,
                    description = i.Description ?? "Gelir Kaydı",
                    categoryId = i.CategoryId,
                    categoryName = i.Category != null ? i.Category.Name : "Genel",
                    categoryColor = i.Category != null ? i.Category.Color : "#10b981",
                    date = i.Date.ToString("dd.MM.yyyy"),
                    time = i.Date.ToString("HH:mm"),
                    rawDate = i.Date,
                    isRecurring = i.IsRecurring
                })
                .ToListAsync();

            return Ok(incomes);
        }

        // --- 9. AYLIK GİDER RAPORU ---
        [HttpGet("expense-report")]
        public async Task<IActionResult> GetExpenseReport([FromQuery] int month, [FromQuery] int? year)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            int targetYear = year ?? DateTime.UtcNow.Year;
            var start = new DateTime(targetYear, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddTicks(-1);

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.Date >= start && e.Date <= end)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    id = e.Id,
                    amount = e.Amount,
                    description = e.Description ?? "Gider Kaydı",
                    categoryId = e.CategoryId,
                    categoryName = e.Category != null ? e.Category.Name : "Genel",
                    categoryColor = e.Category != null ? e.Category.Color : "#ef4444",
                    date = e.Date.ToString("dd.MM.yyyy"),
                    time = e.Date.ToString("HH:mm"),
                    rawDate = e.Date,
                    isRecurring = e.IsRecurring
                })
                .ToListAsync();

            return Ok(expenses);
        }

        // --- 10. KART EKSTRE DETAYI (AKILLI DÖNEM ALGORİTMASI) ---
        [HttpGet("card-statement/{cardId}")]
        public async Task<IActionResult> GetCardStatement(int cardId, [FromQuery] int? month, [FromQuery] int? year)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
            if (card == null) return NotFound("Kart bulunamadı.");

            DateTime startDate, endDate;
            var now = DateTime.UtcNow;

            // KANKA BAKIYORUZ: Eğer kullanıcı manuel ay/yıl seçmediyse otomatik hesapla
            if (!month.HasValue || !year.HasValue)
            {
                if (now.Day > card.ClosingDay)
                {
                    // SENARYO 1: Gün 19 Nisan, Kesim 18 Nisan. 
                    // Aralık: 19 Nisan - 18 Mayıs
                    int safeStartDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(now.Year, now.Month));
                    startDate = new DateTime(now.Year, now.Month, safeStartDay, 0, 0, 0, DateTimeKind.Utc).AddDays(1);

                    var nextMonth = now.AddMonths(1);
                    int safeEndDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    endDate = new DateTime(nextMonth.Year, nextMonth.Month, safeEndDay, 23, 59, 59, DateTimeKind.Utc);
                }
                else
                {
                    // SENARYO 2: Gün 17 Nisan, Kesim 18 Nisan.
                    // Aralık: 19 Mart - 18 Nisan
                    int safeEndDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(now.Year, now.Month));
                    endDate = new DateTime(now.Year, now.Month, safeEndDay, 23, 59, 59, DateTimeKind.Utc);

                    var prevMonth = now.AddMonths(-1);
                    int safeStartDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month));
                    startDate = new DateTime(prevMonth.Year, prevMonth.Month, safeStartDay, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
                }
            }
            else
            {
                // Kullanıcı takvimden özel bir ay seçtiyse o ayı getir
                int targetMonth = month.Value;
                int targetYear = year.Value;
                int safeClosingDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(targetYear, targetMonth));
                endDate = new DateTime(targetYear, targetMonth, safeClosingDay, 23, 59, 59, DateTimeKind.Utc);

                var prev = endDate.AddMonths(-1);
                startDate = new DateTime(prev.Year, prev.Month, Math.Min(card.ClosingDay, DateTime.DaysInMonth(prev.Year, prev.Month)), 0, 0, 0, DateTimeKind.Utc).AddDays(1);
            }

            // TransactionController.cs içinde 10. metodu bul ve şu şekilde güncelle:
            var statementItems = await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.CreditCardId == cardId && e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    id = e.Id,
                    date = e.Date.ToString("dd.MM.yyyy"),
                    description = e.Description,
                    categoryId = e.CategoryId, 
                    categoryName = e.Category != null ? e.Category.Name : "Genel",
                    categoryColor = e.Category != null ? e.Category.Color : "#3b82f6",
                    amount = e.Amount,
                    isPaid = e.IsPaid,
                    rawDate = e.Date // Düzenleme için bu da lazım
                })
                .ToListAsync();

            return Ok(new
            {
                cardName = card.CardName,
                closingDay = card.ClosingDay,
                periodStart = startDate.ToString("dd.MM.yyyy"),
                periodEnd = endDate.ToString("dd.MM.yyyy"),
                items = statementItems,
                totalAmount = statementItems.Where(x => !x.description.Contains("Kart Ödemesi")).Sum(x => x.amount)
            });
        }

        [HttpGet("cash-expenses")]
        public async Task<IActionResult> GetCashExpenses()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.CreditCardId == null)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    id = e.Id,
                    description = e.Description,
                    amount = e.Amount,
                    date = e.Date,
                    categoryName = e.Category != null ? e.Category.Name : "Genel",
                    categoryColor = e.Category != null ? e.Category.Color : "#cbd5e1"
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpDelete("expense/{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense == null) return NotFound();

            if (expense.CreditCardId != null)
            {
                var card = await _context.CreditCards.FindAsync(expense.CreditCardId);
                if (card != null) card.CurrentDebt -= expense.Amount;
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // =======================================================================
        // --- 11. YAPAY ZEKA İLE SADECE DOSYA ÇÖZÜMLEME VE EKLEME ---
        // =======================================================================
        [HttpPost("ai-parse-file")]
        public async Task<IActionResult> ParseFromAi([FromForm] IFormFile file, [FromForm] int type, [FromForm] int? creditCardId)
        {
            int userId = GetUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Oturum geçersiz. Lütfen tekrar giriş yapın." });

            // KANKA: Metin girişini kaldırdık, artık dosya zorunlu!
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Lütfen analiz edilecek bir ekstre dosyası (PDF/Görsel) yükleyin kanka." });
            }

            try
            {
                // 1. ADIM: DOSYA KONTROLLERİ VE BYTE DİZİSİNE ÇEVİRME
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 10MB sınırını aşamaz." });

                string fileExtension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = "Sadece PDF, PNG veya JPG yükleyebilirsin." });

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                // 2. ADIM: AI'DAN VERİLERİ ALIYORUZ (Text parametresine boş string geçiyoruz)
                var parsedTransactions = await _aiService.ExtractTransactionsAsync(fileBytes, fileExtension, "");

                if (parsedTransactions == null || !parsedTransactions.Any())
                    return Ok(new { message = "Dosyada kaydedilecek geçerli bir harcama bulunamadı." });

                // 3. ADIM: VERİTABANINDAKİ MEVCUT KAYITLARI ÇEKİYORUZ (Mükerrer kontrolü için)
                var recentDateLimit = DateTime.UtcNow.AddMonths(-3);
                var existingTransactions = await _context.Expenses
                    .AsNoTracking()
                    .Where(e => e.UserId == userId && e.CreditCardId == creditCardId && e.Date >= recentDateLimit)
                    .Select(e => new { e.Date, e.Amount })
                    .ToListAsync();

                var newTransactionsToSave = new List<Expense>();
                decimal totalNewAmount = 0;

                // 4. ADIM: AKILLI SÜZGEÇ (Tarih ve 5 Kuruş Toleranslı Tutar Kontrolü)
                foreach (var parsed in parsedTransactions)
                {
                    bool isDuplicate = existingTransactions.Any(ex =>
                        ex.Date.Date == parsed.Date.Date &&
                        Math.Abs(ex.Amount - parsed.Amount) <= 0.05m)
                        ||
                        newTransactionsToSave.Any(nt =>
                        nt.Date.Date == parsed.Date.Date &&
                        Math.Abs(nt.Amount - parsed.Amount) <= 0.05m);

                    if (!isDuplicate)
                    {
                        parsed.UserId = userId;
                        parsed.CreditCardId = creditCardId;

                        // Kategori bulunamadıysa 'Diğer' (106) yapıyoruz
                        if (parsed.CategoryId == 0) parsed.CategoryId = 106;

                        newTransactionsToSave.Add(parsed);
                        totalNewAmount += parsed.Amount;
                    }
                }

                // 5. ADIM: KAYIT VE KART BORCU GÜNCELLEME
                if (newTransactionsToSave.Any())
                {
                    await _context.Expenses.AddRangeAsync(newTransactionsToSave);

                    if (creditCardId.HasValue)
                    {
                        var card = await _context.CreditCards.FindAsync(creditCardId.Value);
                        if (card != null)
                        {
                            card.CurrentDebt += totalNewAmount;
                        }
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = $"{newTransactionsToSave.Count} adet yeni işlem başarıyla eklendi!",
                        addedCount = newTransactionsToSave.Count
                    });
                }

                return Ok(new { message = "Yüklediğin dosyadaki tüm işlemler zaten sistemde kayıtlı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İşlem sırasında bir hata oluştu: " + ex.Message });
            }
        }
    }
}