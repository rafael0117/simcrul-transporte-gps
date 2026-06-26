namespace SIMCRUL.Entity;

public class PasswordResetToken
{
    public int IdPasswordResetToken { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpirationUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RequestedByIp { get; set; }
    public string EmailSentTo { get; set; } = string.Empty;

    public virtual Usuario Usuario { get; set; } = null!;
}
