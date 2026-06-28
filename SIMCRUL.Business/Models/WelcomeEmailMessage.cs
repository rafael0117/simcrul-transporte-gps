namespace SIMCRUL.Business.Models;

public class WelcomeEmailMessage
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
