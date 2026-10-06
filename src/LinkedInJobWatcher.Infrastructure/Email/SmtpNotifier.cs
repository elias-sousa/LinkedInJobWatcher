using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LinkedInJobWatcher.Infrastructure.Email;

public sealed class SmtpNotifier(IOptions<EmailOptions> options, ILogger<SmtpNotifier> logger) : INotifier
{
    private readonly EmailOptions _options = options.Value;

    public Task SendNewItemsAsync(IReadOnlyList<ScoredItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Task.CompletedTask;
        }

        var (subject, html, text) = EmailTemplateBuilder.BuildNewItemsEmail(items);
        return SendAsync(subject, html, text, cancellationToken);
    }

    public Task SendSessionExpiredAlertAsync(CancellationToken cancellationToken)
    {
        var (subject, html, text) = EmailTemplateBuilder.BuildSessionExpiredEmail();
        return SendAsync(subject, html, text, cancellationToken);
    }

    public Task SendBlockedAlertAsync(string reason, TimeSpan backoffDuration, CancellationToken cancellationToken)
    {
        var (subject, html, text) = EmailTemplateBuilder.BuildBlockedEmail(reason, backoffDuration);
        return SendAsync(subject, html, text, cancellationToken);
    }

    public Task SendTestEmailAsync(CancellationToken cancellationToken)
    {
        var (subject, html, text) = EmailTemplateBuilder.BuildTestEmail();
        return SendAsync(subject, html, text, cancellationToken);
    }

    private async Task SendAsync(string subject, string html, string text, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(_options.ToAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody();

        using var client = new SmtpClient();

        var socketOptions = _options.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.UseSsl ? socketOptions : SecureSocketOptions.None, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("E-mail enviado: {Subject}", subject);
    }
}
