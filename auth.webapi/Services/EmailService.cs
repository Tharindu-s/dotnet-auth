using System.Diagnostics.CodeAnalysis;
using auth.webapi.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace auth.webapi.Services
{
    // Sends email over SMTP. SMTP is optional: without EmailSettings:From, :Username and :Password,
    // emails are logged and skipped, so the service works out of the box.
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string? _fromEmail;
        private readonly string? _userName;
        private readonly string? _fromPassword;
        private readonly int _port;
        private readonly string? _smtpServer;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _logger = logger;
            _fromEmail = config["EmailSettings:From"];
            _userName = config["EmailSettings:Username"];
            _fromPassword = config["EmailSettings:Password"];
            _smtpServer = config["EmailSettings:SmtpServer"];
            _port = config.GetValue("EmailSettings:Port", 587);
        }

        [MemberNotNullWhen(true, nameof(_fromEmail), nameof(_userName), nameof(_fromPassword), nameof(_smtpServer))]
        private bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_fromEmail) && !string.IsNullOrWhiteSpace(_userName) &&
            !string.IsNullOrWhiteSpace(_fromPassword) && !string.IsNullOrWhiteSpace(_smtpServer);

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (!IsConfigured)
            {
                _logger.LogInformation("SMTP isn't configured, so the email \"{Subject}\" to {Email} wasn't sent.", subject, toEmail);
                return;
            }

            try
            {
                _logger.LogInformation("Sending email to {Email}", toEmail);

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_fromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_smtpServer, _port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_userName, _fromPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                _logger.LogInformation("Email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            }
        }
    }
}
