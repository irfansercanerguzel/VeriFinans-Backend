using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace VeriFinans.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var apiKey = _configuration["EmailSettings:MailjetApiKey"];
            var apiSecret = _configuration["EmailSettings:MailjetApiSecret"];
            var senderEmail = _configuration["EmailSettings:Email"] ?? "medsched0@gmail.com";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("--> Mailjet şifreleri bulunamadı! Ayarları kontrol et.");
                return;
            }

            try
            {
                // Mailjet'in HTTP API'sine doğrudan bağlanıyoruz (Render bu portu kapatamaz)
                using var client = new HttpClient();

                // Şifreleri Mailjet'in istediği formata (Basic Auth) çeviriyoruz
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                // Mailjet'in istediği JSON yapısını kuruyoruz
                var payload = new
                {
                    Messages = new[]
                    {
                        new
                        {
                            From = new { Email = senderEmail, Name = "VeriFinans AI" },
                            To = new[] { new { Email = toEmail } },
                            Subject = subject,
                            HTMLPart = message
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Roketi ateşliyoruz
                var response = await client.PostAsync("https://api.mailjet.com/v3.1/send", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("--> Mailjet ile mail başarıyla Render engelini aşıp fırlatıldı! 🚀");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"--> Mailjet Hatası: Status {response.StatusCode}, Detay: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Kodu çalıştırırken hata oluştu: {ex.Message}");
            }
        }
    }
}