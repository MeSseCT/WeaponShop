using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeaponShop.Application.Interfaces;

namespace WeaponShop.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP host not configured. Skipping email to {Email}.", toEmail);
            return;
        }

        var fromAddress = _configuration["Email:FromAddress"] ?? "no-reply@weaponshop.local";
        var fromName = _configuration["Email:FromName"] ?? "WeaponShop";
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var smtpPort) ? smtpPort : 587;
        var user = _configuration["Email:Smtp:User"];
        var password = _configuration["Email:Smtp:Password"];
        var enableSsl = bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var ssl) && ssl;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(user, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
