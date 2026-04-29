using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VeriFinans.Services;
using VeriFinans.DTOs; // DTO sınıfımızı içeri aktarıyoruz

namespace VeriFinans.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly AiService _aiService;

        public AiController(AiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeData([FromBody] AiAnalyzeRequestDto request)
        {
            try
            {
                // Frontend zaten DataJson parametresiyle veriyi metin olarak yolluyor.
                // Bu yüzden JsonConvert ile tekrar çevirmeye gerek kalmadan doğrudan servise iletiyoruz.
                string aiResult = await _aiService.AskAssistantAsync(request.ActionType, request.DataJson);

                return Ok(new { message = aiResult });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Sistem Hatası: {ex.Message}" });
            }
        }
    }
}