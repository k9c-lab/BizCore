using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace BizCore.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody)
    {
        var toAddresses = recipients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (toAddresses.Count == 0)
        {
            _logger.LogInformation("Email notification skipped because no recipient email address was found.");
            return;
        }

        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email notification skipped because Email.Enabled is false. Subject: {Subject}", subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            _logger.LogWarning("Email notification skipped because SMTP host or from address is not configured.");
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var address in toAddresses)
            {
                message.To.Add(address);
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
            }

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email notification failed. Subject: {Subject}", subject);
        }
    }
}
