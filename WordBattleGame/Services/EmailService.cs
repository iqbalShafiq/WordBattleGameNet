using MailKit.Net.Smtp;
using MimeKit;

namespace WordBattleGame.Services
{
    public class EmailService(IConfiguration config) : IEmailService
    {
        private readonly IConfiguration _config = config;

        public async Task SendEmailAsync(string subject, string content, string toEmail, string confirmationLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("WordBattle", _config["Email:From"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = $"{content}:\n{confirmationLink}"
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
