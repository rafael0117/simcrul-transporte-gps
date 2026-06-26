namespace SIMCRUL.Business.Security;

public class PasswordRecoveryOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SIMCRUL";
    public string FrontendResetUrl { get; set; } = "http://localhost:5171/Cuenta/RestablecerContrasena";
    public bool UseSsl { get; set; } = true;
    public int TokenExpiryMinutes { get; set; } = 30;
    public string[] AlertRecipientEmails { get; set; } = Array.Empty<string>();
    public bool SendAlertsToCompanyEmail { get; set; } = true;
}
