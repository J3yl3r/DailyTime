using System.Net;
using System.Net.Mail;
using dailyTimeWorker.Configuration;
using dailyTimeWorker.Models;
using Microsoft.Extensions.Options;

namespace dailyTimeWorker.Services.Notifications;

public interface INotificationService
{
    Task<NotificationResult> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
    NotificationResult GetStatus();
}

public class NotificationService : INotificationService
{
    private readonly NotificationsOptions _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<NotificationsOptions> options,
        ILogger<NotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public NotificationResult GetStatus() => new()
    {
        Sent = false,
        Message = _options.Smtp.Enabled
            ? $"SMTP listo ({_options.Smtp.Host}:{_options.Smtp.Port})"
            : "SMTP desactivado. Configura Notifications:Smtp en appsettings."
    };

    public async Task<NotificationResult> SendEmailAsync(
        SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Smtp.Enabled)
        {
            return new NotificationResult
            {
                Sent = false,
                Message = "SMTP desactivado. Activa Notifications:Smtp:Enabled y completa Host/User/Password."
            };
        }

        if (string.IsNullOrWhiteSpace(request.To) ||
            string.IsNullOrWhiteSpace(request.Subject))
        {
            return new NotificationResult
            {
                Sent = false,
                Message = "To y Subject son obligatorios."
            };
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.Smtp.From),
                Subject = request.Subject,
                Body = request.Body ?? string.Empty,
                IsBodyHtml = request.IsHtml
            };
            message.To.Add(request.To);

            using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
            {
                EnableSsl = _options.Smtp.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.Smtp.User))
            {
                client.Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Correo enviado a {To}", request.To);
            return new NotificationResult { Sent = true, Message = "Correo enviado." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo a {To}", request.To);
            return new NotificationResult { Sent = false, Message = ex.Message };
        }
    }
}
