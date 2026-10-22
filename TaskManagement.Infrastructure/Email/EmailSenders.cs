using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Infrastructure.Settings;

namespace TaskManagement.Infrastructure.Email;

/// <summary>Mock notification channel used when SMTP is not configured: the email is written to the application log.</summary>
public sealed class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger) => _logger = logger;

    public Task<bool> SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK EMAIL] To: {Name} <{Email}> | Subject: {Subject} | Body: {Body}", toName, toEmail, subject, body);
        return Task.FromResult(true);
    }
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(toEmail, toName));

        using var client = new SmtpClient(_settings.Host, _settings.Port) { EnableSsl = _settings.EnableSsl };
        if (!string.IsNullOrEmpty(_settings.UserName))
            client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);

        await client.SendMailAsync(message, ct);
        _logger.LogInformation("Email '{Subject}' sent to {Email}", subject, toEmail);
        return true;
    }
}
