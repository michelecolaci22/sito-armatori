using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebAppColax.Services;

public enum EmailSendResult
{
    Sent,
    NotConfigured,
    Failed
}

public sealed class EmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        string subject,
        string body,
        string replyTo,
        string replyToName,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Contatti:SmtpHost"]?.Trim();
        var from = _configuration["Contatti:From"]?.Trim();
        var to = _configuration["Contatti:To"]?.Trim();
        var user = _configuration["Contatti:SmtpUser"]?.Trim();
        var password = _configuration["Contatti:SmtpPassword"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(to) ||
            (!string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(password)) ||
            (string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password)))
        {
            _logger.LogWarning("Invio email non disponibile: configurazione SMTP incompleta.");
            return EmailSendResult.NotConfigured;
        }

        if (!int.TryParse(_configuration["Contatti:SmtpPort"], out var port) || port is < 1 or > 65535)
        {
            _logger.LogWarning("Invio email non disponibile: porta SMTP non valida.");
            return EmailSendResult.NotConfigured;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from, "Armatori Salentini"),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(to));
            message.ReplyToList.Add(new MailAddress(replyTo, replyToName));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = !bool.TryParse(_configuration["Contatti:EnableSsl"], out var enableSsl) || enableSsl,
                UseDefaultCredentials = false,
                Timeout = 15000
            };

            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, password);

            await client.SendMailAsync(message, cancellationToken);
            return EmailSendResult.Sent;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invio email SMTP non riuscito.");
            return EmailSendResult.Failed;
        }
    }
}
