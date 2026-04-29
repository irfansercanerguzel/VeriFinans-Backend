using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VeriFinans.BackgroundServices;
using VeriFinans.Data;
using VeriFinans.Services;

// 1. POSTGRESQL TARİH HATASI ÇÖZÜMÜ
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --- VERİTABANI BAĞLANTISI (RETRY MEKANİZMASI EKLENDİ) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
    npgsqlOptions =>
    {
        // Transient Failure hatası aldığında pes etme, 5 kez tekrar dene.
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// --- JWT AYARLARI ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyStr = jwtSettings["Key"] ?? "SercaninCokGizliVeCokGucluSifresi123!";
var key = Encoding.ASCII.GetBytes(keyStr);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

// --- SERVİS KAYITLARI ---
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddHostedService<FinanceEmailWorker>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- CORS POLİTİKASI (GÜNCELLENDİ) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("VeriFinansPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://veri-finans-frontend.vercel.app" // Vercel linkini buraya ekledik
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
var app = builder.Build();

// --- MIDDLEWARE SIRALAMASI ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("VeriFinansPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();