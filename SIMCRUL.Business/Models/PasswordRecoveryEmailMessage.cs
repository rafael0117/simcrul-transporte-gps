namespace SIMCRUL.Business.Models;

public class PasswordRecoveryEmailMessage
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string ResetUrl { get; set; } = string.Empty;
    public DateTime ExpirationUtc { get; set; }
}
