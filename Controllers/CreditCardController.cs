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
using VeriFinans.Dtos;

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CreditCardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CreditCardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- YARDIMCI METOD ---
        private int GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // --- 1. KARTLARI LİSTELE ---
        [HttpGet]
        public async Task<IActionResult> GetMyCards()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var cards = await _context.CreditCards
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CardName)
                .ToListAsync();

            return Ok(cards);
        }

        // --- 2. YENİ KART TANIMLA ---
        [HttpPost]
        public async Task<IActionResult> CreateCard([FromBody] CreditCard card)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            card.UserId = userId;
            card.CurrentDebt = 0;

            _context.CreditCards.Add(card);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kredi kartı başarıyla tanımlandı.", card });
        }

        // --- 3. KART GÜNCELLE ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCard(int id, [FromBody] CreditCard updatedCard)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var card = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (card == null) return NotFound("İlgili kart kaydı bulunamadı.");

            card.CardName = updatedCard.CardName;
            card.ClosingDay = updatedCard.ClosingDay;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Kart bilgileri güncellendi.", card });
        }

        // --- 4. KART SİL ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (card == null) return NotFound();

            _context.CreditCards.Remove(card);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kart silindi." });
        }

        // --- 5. KART ÖDEMESİ YAP (Kurşun Geçirmez Versiyon) ---
        [HttpPost("pay")]
        public async Task<IActionResult> PayCardDebt([FromBody] CardPaymentDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Kartı bul
                    var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == dto.CardId && c.UserId == userId);
                    if (card == null) return NotFound(new { message = "Kart bulunamadı." });

                    // Tutarın her zaman pozitif gelmesini garantiye alıyoruz
                    decimal payAmount = Math.Abs(dto.Amount);

                    // ===============================================================
                    // YENİ EKLENEN KISIM: EKSTRE HARCAMALARINI BUL VE ÖDENDİ YAP
                    // ===============================================================
                    var now = DateTime.UtcNow;
                    int safeDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(now.Year, now.Month));
                    DateTime thisMonthClosingDate = new DateTime(now.Year, now.Month, safeDay, 23, 59, 59, DateTimeKind.Utc);

                    DateTime lastStatementDate;
                    if (now >= thisMonthClosingDate)
                    {
                        lastStatementDate = thisMonthClosingDate;
                    }
                    else
                    {
                        var lastMonth = now.AddMonths(-1);
                        int safeLastDay = Math.Min(card.ClosingDay, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                        lastStatementDate = new DateTime(lastMonth.Year, lastMonth.Month, safeLastDay, 23, 59, 59, DateTimeKind.Utc);
                    }

                    // Sadece tarihi son kesim tarihinden küçük/eşit olan ve IsPaid false olan harcamaları çek
                    var expensesToPay = await _context.Expenses
                        .Where(e => e.CreditCardId == card.Id
                                 && e.Date <= lastStatementDate
                                 && e.IsPaid == false)
                        .ToListAsync();

                    // Hepsini ödendi olarak işaretle
                    foreach (var exp in expensesToPay)
                    {
                        exp.IsPaid = true;
                    }
                    // ===============================================================

                    // 2. Güncel borcu düşür
                    card.CurrentDebt -= payAmount;
                    if (card.CurrentDebt < 0) card.CurrentDebt = 0;

                    // EF Core'a kartın güncellendiğini zorla bildiriyoruz
                    _context.CreditCards.Update(card);

                    // 3. Bütçenin dengesi için "Nakit Gider" olarak kaydet
                    var paymentExpense = new Expense
                    {
                        UserId = userId,
                        Amount = payAmount,
                        Description = $"{card.CardName} Kart Ödemesi",
                        Date = DateTime.UtcNow,
                        CategoryId = 108, 
                        CreditCardId = null, // Kesinlikle null kalmalı!
                        InstallmentCount = 1,
                        CurrentInstallment = 1,
                        IsRecurring = false
                    };

                    _context.Expenses.Add(paymentExpense);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Frontend'in doğru rakamı görmesi için yeni borcu da dönüyoruz
                    return Ok(new { message = "Ödeme başarıyla alındı ve harcamalar ödendi olarak işaretlendi.", newDebt = card.CurrentDebt });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Ödeme sırasında hata oluştu.", error = ex.Message });
                }
            });
        }
    }
}