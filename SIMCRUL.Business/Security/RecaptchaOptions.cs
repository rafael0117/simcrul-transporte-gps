namespace SIMCRUL.Business.Security;

public class RecaptchaOptions
{
    public bool Enabled { get; set; }
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    public decimal MinimumScore { get; set; } = 0.5M;
    public string LoginAction { get; set; } = "login";
    public string Version { get; set; } = "v2";
}
