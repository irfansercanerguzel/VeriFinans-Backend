using System.Net;
using System.Net.Mail;

namespace VeriFinans.Services
{
    public class EmailService
    {
   
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("medsched0@gmail.com", "tfkqemnemketvxwr")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("medsched0@gmail.com", "VeriFinance AI"),
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