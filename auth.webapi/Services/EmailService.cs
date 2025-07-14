using auth.webapi.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace auth.webapi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfigurationHelperService _config;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _userName;
        private readonly string _fromPassword;
        private readonly string _port;
        private readonly string _smtpServer;
        public EmailService(IConfigurationHelperService config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
            _fromEmail = config.GetRequiredConfig("EmailSettings:From");
            _userName = config.GetRequiredConfig("EmailSettings:Username");
            _fromPassword = config.GetRequiredConfig("EmailSettings:Password");
            _port = config.GetRequiredConfig("EmailSettings:Port");
            _smtpServer = config.GetRequiredConfig("EmailSettings:SmtpServer");

        }
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                _logger.LogInformation("Sending email to {Email}", toEmail);

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_fromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                   _smtpServer,
                    int.Parse(_port),
                    SecureSocketOptions.StartTls
                );
                _logger.LogInformation("Connected to SMTP server...");

                await smtp.AuthenticateAsync(
                    _userName,
                    _fromPassword
                );
                await smtp.SendAsync(email);
                _logger.LogInformation("Email sent successfully!");
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            }
        }
    }
}