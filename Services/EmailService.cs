using System.Net;
using System.Net.Mail;

namespace VeriFinans.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        // IConfiguration'ı inject ediyoruz ki ayarlara ulaşabilelim
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Ayarları Render'dan (veya appsettings'ten) çekiyoruz
            var senderEmail = _configuration["EmailSettings:Email"] ?? "medsched0@gmail.com";
            var senderPassword = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(senderPassword))
            {
                Console.WriteLine("--> Mail şifresi bulunamadı! Ayarları kontrol et.");
                return;
            }

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, senderPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "VeriFinance AI"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mailMessage);
                Console.WriteLine("--> Mail başarıyla gönderildi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Mail gönderme hatası: {ex.Message}");
            }
        }
    }
}