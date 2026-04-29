using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VeriFinans.Data;
using VeriFinans.DTOs;
using VeriFinans.Models;

namespace VeriFinans.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. Kullanıcıyı bul
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // 2. Kullanıcı yoksa veya şifre yanlışsa (Güvenlik için hata mesajını genel veriyoruz)
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Email veya şifre hatalı girildi!" });
            }

            // 3. Token üret
            var token = GenerateJwtToken(user);

            // 4. Frontend'in beklediği formatta dön
            return Ok(new
            {
                token = token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // 1. Gelen veriyi kontrol et (frontend'den fullName geliyor olabilir, DTO'na göre eşle)
            if (string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
            {
                return BadRequest(new { message = "Tüm alanları doldurun lütfen" });
            }

            // 2. Email zaten var mı?
            if (await _context.User.AnyAsync(u => u.Email == registerDto.Email))
            {
                return BadRequest(new { message = "Bu email zaten sistemde mevcut" });
            }

            // 3. Şifreyi Hashle
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // 4. Kaydet
            var user = new User
            {
                Name = registerDto.Name, // DTO'nda 'Name' olarak tanımlı olduğunu varsayıyorum
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kayıt başarılı!" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyStr = jwtSettings["Key"];

            if (string.IsNullOrEmpty(keyStr))
                throw new Exception("JWT Key appsettings.json içerisinde bulunamadı!");

            var key = Encoding.ASCII.GetBytes(keyStr);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "User"),
                    new Claim("Name", user.Name ?? "")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}