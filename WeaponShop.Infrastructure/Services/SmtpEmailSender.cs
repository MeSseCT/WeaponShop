using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
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

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = false,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP server není nastaven. Odeslání e-mailu na adresu {Email} bylo přeskočeno.", toEmail);
            return;
        }

        var fromAddress = _configuration["Email:FromAddress"] ?? "no-reply@weaponshop.local";
        var fromName = _configuration["Email:FromName"] ?? "Zbrojnice";
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var smtpPort) ? smtpPort : 465;
        var user = _configuration["Email:Smtp:User"];
        var password = _configuration["Email:Smtp:Password"];
        var enableSsl = bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var ssl) && ssl;
        var checkCertificateRevocation = !bool.TryParse(_configuration["Email:Smtp:CheckCertificateRevocation"], out var skipRevocation)
            || skipRevocation;

        if (!string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP účet {User} nemá vyplněné heslo. Odeslání e-mailu na adresu {Email} bylo přeskočeno.", user, toEmail);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }

            if (attachments is not null)
            {
                foreach (var attachment in attachments.Where(item => item.Content.Length > 0))
                {
                    builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = checkCertificateRevocation;
            var socketOptions = enableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(host, port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
            {
                await client.AuthenticateAsync(user, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Nepodařilo se odeslat e-mail na adresu {Email} se serverem {Host}:{Port} a předmětem {Subject}.",
                toEmail,
                host,
                port,
                subject);
        }
    }
}
