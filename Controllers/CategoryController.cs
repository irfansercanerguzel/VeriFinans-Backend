using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // 1. ANA KATEGORİLERİ GETİR
        [HttpGet("main")]
        public async Task<IActionResult> GetMainCategories([FromQuery] int type)
        {
            var categories = await _context.Categories
                .Where(c => c.Level == 1 && c.Type == type)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        // 2. ALT KATEGORİLERİ GETİR
        [HttpGet("sub/{parentId}")]
        public async Task<IActionResult> GetSubCategories(int parentId)
        {
            var subCategories = await _context.Categories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(subCategories);
        }

        // --- 3. YENİ: ZİNCİRLEME KATEGORİ OLUŞTURMA (Frontend'deki '+' butonu için) ---
        [HttpPost("chain")]
        public async Task<IActionResult> CreateCategoryChain([FromBody] CategoryChainDto dto)
        {
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

                    // Bu seviyede, bu isimde ve bu üst kategoriye bağlı bir kayıt var mı?
                    var existingCat = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == safeName.ToLower() &&
                                                  c.Type == dto.Type &&
                                                  c.Level == currentLevel &&
                                                  c.ParentId == currentParentId);

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
                            ParentId = currentParentId
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
            return Ok(await _context.Categories.ToListAsync());
        }
    }

}