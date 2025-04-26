using MailKit.Net.Smtp;
using MimeKit;

namespace WordBattleGame.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);
    }

    public class EmailService(IConfiguration config) : IEmailService
    {
        private readonly IConfiguration _config = config;

        public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("WordBattle", _config["Email:From"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Konfirmasi Email WordBattle";
            message.Body = new TextPart("plain")
            {
                Text = $"Klik link berikut untuk konfirmasi email: {confirmationLink}"
            };

            using var client = new SmtpClient();
            var smtpPort = _config["Email:SmtpPort"];
            if (string.IsNullOrEmpty(smtpPort))
            {
                throw new InvalidOperationException("SMTP port configuration is missing.");
            }
            await client.ConnectAsync(_config["Email:SmtpHost"], int.Parse(smtpPort), true);
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
