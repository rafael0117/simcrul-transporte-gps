using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Business.Models;
using SIMCRUL.Business.Security;

namespace SIMCRUL.Business.Services;

public class SmtpEmailService : IEmailService
{
    private readonly PasswordRecoveryOptions _options;

    public SmtpEmailService(IOptions<PasswordRecoveryOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPasswordRecoveryEmailAsync(PasswordRecoveryEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = new StringBuilder()
            .AppendLine("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#0f172a;max-width:640px\">")
            .AppendLine("<h2 style=\"color:#0f4c81;margin-bottom:12px\">Recuperacion de contrasena - SIMCRUL</h2>")
            .AppendLine($"<p>Hola {WebUtility.HtmlEncode(message.RecipientName)},</p>")
            .AppendLine("<p>Recibimos una solicitud para restablecer tu contrasena del sistema SIMCRUL.</p>")
            .AppendLine($"<p><a href=\"{message.ResetUrl}\" style=\"display:inline-block;background:#0f4c81;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px\">Restablecer contrasena</a></p>")
            .AppendLine($"<p>Este enlace expirara el {message.ExpirationUtc.ToLocalTime():dd/MM/yyyy HH:mm}.</p>")
            .AppendLine("<p>Si no solicitaste este cambio, puedes ignorar este correo.</p>")
            .AppendLine("</div>")
            .ToString();

        await SendEmailAsync(message.RecipientEmail, "SIMCRUL - Recuperacion de contrasena", body, cancellationToken);
    }

    public async Task SendAlertEmailAsync(AlertEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var latLng = message.Latitude.HasValue && message.Longitude.HasValue
            ? $"{message.Latitude}, {message.Longitude}"
            : "No disponible";

        var body = new StringBuilder()
            .AppendLine("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#0f172a;max-width:720px\">")
            .AppendLine("<h2 style=\"color:#b91c1c;margin-bottom:12px\">Alerta operativa SIMCRUL</h2>")
            .AppendLine("<p>Se registro una nueva alerta en el sistema de monitoreo.</p>")
            .AppendLine("<table style=\"border-collapse:collapse;width:100%\">")
            .AppendLine(Row("Tipo", message.AlertType))
            .AppendLine(Row("Severidad", $"Nivel {message.Severity}"))
            .AppendLine(Row("Vehiculo", $"{message.VehiclePlate} / {message.VehicleCode}"))
            .AppendLine(Row("Ruta", message.RouteName))
            .AppendLine(Row("Conductor", message.DriverName))
            .AppendLine(Row("Fecha UTC", message.AlertDateUtc.ToString("dd/MM/yyyy HH:mm:ss")))
            .AppendLine(Row("Ubicacion", latLng))
            .AppendLine(Row("Descripcion", message.Description))
            .AppendLine("</table>")
            .AppendLine("</div>")
            .ToString();

        foreach (var recipient in message.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await SendEmailAsync(recipient, $"SIMCRUL - Alerta {message.AlertType}", body, cancellationToken);
        }
    }

    private async Task SendEmailAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var smtp = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            UseDefaultCredentials = false,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(
                _options.SmtpUser.Trim(),
                NormalizeSecret(_options.SmtpPassword))
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(recipient);
        await smtp.SendMailAsync(mail, cancellationToken);
    }

    private static string Row(string label, string value)
    {
        return $"<tr><td style=\"padding:8px;border:1px solid #cbd5e1;background:#f8fafc;font-weight:600;width:180px\">{WebUtility.HtmlEncode(label)}</td><td style=\"padding:8px;border:1px solid #cbd5e1\">{WebUtility.HtmlEncode(value)}</td></tr>";
    }

    private static string NormalizeSecret(string secret)
    {
        return string.Concat(secret.Where(c => !char.IsWhiteSpace(c)));
    }
}
