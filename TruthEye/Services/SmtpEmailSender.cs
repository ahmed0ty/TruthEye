using System.Net;
using System.Net.Mail;

namespace TruthEye.Services
{
    /// <summary>بيبعت البريد عبر SMTP. الإعدادات من Smtp:* (والباسورد من user-secrets).</summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var from = _config["Smtp:From"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Email is not configured (Smtp:Host / Smtp:From)");

            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];

            if (string.IsNullOrWhiteSpace(password))
                _logger.LogWarning("Smtp:Password is not set in user-secrets; the SMTP server will reject the login");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };
            using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent: {Subject}", subject);
            }
            catch (Exception ex)
            {
                // The password is never logged; only the server, port and account name
                _logger.LogError(ex, "SMTP send failed via {Host}:{Port} as {User}", host, port, username);
                throw;
            }
        }
    }
}
