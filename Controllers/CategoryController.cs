using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeriFinans.Data;
using VeriFinans.Models;
using VeriFinans.Dtos;

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- YARDIMCI METOD: Giriş yapan kullanıcının ID'sini alır ---
        private int GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // 1. ANA KATEGORİLERİ GETİR (Filtreli)
        [HttpGet("main")]
        public async Task<IActionResult> GetMainCategories([FromQuery] int type)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var categories = await _context.Categories
                .Where(c => c.Level == 1 && c.Type == type && c.UserId == userId) // KANKA: Filtre buraya geldi
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        // 2. ALT KATEGORİLERİ GETİR (Filtreli)
        [HttpGet("sub/{parentId}")]
        public async Task<IActionResult> GetSubCategories(int parentId)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var subCategories = await _context.Categories
                .Where(c => c.ParentId == parentId && c.UserId == userId) // KANKA: Filtre buraya da geldi
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(subCategories);
        }

        // --- 3. ZİNCİRLEME KATEGORİ OLUŞTURMA (UserId Atamalı) ---
        [HttpPost("chain")]
        public async Task<IActionResult> CreateCategoryChain([FromBody] CategoryChainDto dto)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                if (dto.Names == null || !dto.Names.Any(n => !string.IsNullOrWhiteSpace(n)))
                    return BadRequest(new { message = "Kategori isimleri boş olamaz." });

                int? currentParentId = null;
                int currentLevel = 1;
                Category lastCategory = null;

                foreach (var name in dto.Names)
                {
                    var safeName = name.Trim();
                    if (string.IsNullOrEmpty(safeName)) continue;

                    // Bu seviyede, bu isimde, bu kullanıcıya ait ve bu üst kategoriye bağlı bir kayıt var mı?
                    var existingCat = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == safeName.ToLower() &&
                                                  c.Type == dto.Type &&
                                                  c.Level == currentLevel &&
                                                  c.ParentId == currentParentId &&
                                                  c.UserId == userId); // Kullanıcı kontrolü

                    if (existingCat != null)
                    {
                        lastCategory = existingCat;
                    }
                    else
                    {
                        var newCat = new Category
                        {
                            Name = safeName,
                            Type = dto.Type,
                            Level = currentLevel,
                            ParentId = currentParentId,
                            UserId = userId // KANKA: Yeni kategoriye sahibini atıyoruz!
                        };
                        _context.Categories.Add(newCat);
                        await _context.SaveChangesAsync();
                        lastCategory = newCat;
                    }

                    currentParentId = lastCategory.Id;
                    currentLevel++;
                }

                return Ok(new { message = "Kategori başarıyla oluşturuldu.", finalCategoryId = lastCategory?.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kategori eklenirken hata!", error = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            // Sadece bu kullanıcıya ait olan her şeyi dök
            return Ok(await _context.Categories.Where(c => c.UserId == userId).ToListAsync());
        }
    }
}