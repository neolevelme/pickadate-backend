using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// SMTP implementation of <see cref="IEmailService"/>. When
/// <see cref="EmailOptions.SmtpHost"/> is empty the service logs the code
/// to the console instead of attempting to send — this keeps local dev
/// working without SMTP credentials.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            _logger.LogInformation(
                "[DEV] Verification code for {Email}: {Code} (SMTP not configured — skipping real send)",
                toEmail, code);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = $"pickadate.me — your code is {code}";

        var body = new BodyBuilder
        {
            TextBody =
                $"Your pickadate.me verification code is: {code}\n\n" +
                "This code expires in 10 minutes. If you did not request it, you can ignore this email.",
            HtmlBody =
                $"<p>Your pickadate.me verification code is:</p>" +
                $"<p style=\"font-size:28px;letter-spacing:4px;font-weight:700\">{code}</p>" +
                $"<p>This code expires in 10 minutes.</p>"
        };
        msg.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword, ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Sent verification code to {Email}", toEmail);
    }
}
