using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Business.Security;

namespace SIMCRUL.Business.Services;

public class GoogleRecaptchaService : IRecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _options;
    private readonly ILogger<GoogleRecaptchaService> _logger;

    public GoogleRecaptchaService(
        HttpClient httpClient,
        IOptions<RecaptchaOptions> options,
        ILogger<GoogleRecaptchaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string? token, string? remoteIp, string expectedAction, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        try
        {
            using var response = await _httpClient.PostAsync(_options.VerifyUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google reCAPTCHA respondio {StatusCode}.", response.StatusCode);
                return false;
            }

            var verification = await response.Content.ReadFromJsonAsync<RecaptchaVerificationResponse>(cancellationToken);
            if (verification?.Success != true)
            {
                _logger.LogWarning("Google reCAPTCHA rechazo el token. Errores: {Errors}", string.Join(",", verification?.ErrorCodes ?? []));
                return false;
            }

            var isV3 = string.Equals(_options.Version, "v3", StringComparison.OrdinalIgnoreCase);

            if (isV3 && verification.Score.HasValue && verification.Score.Value < _options.MinimumScore)
            {
                _logger.LogWarning("Google reCAPTCHA score bajo: {Score}.", verification.Score.Value);
                return false;
            }

            if (isV3 &&
                !string.IsNullOrWhiteSpace(verification.Action) &&
                !string.Equals(verification.Action, expectedAction, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Google reCAPTCHA accion inesperada: {Action}.", verification.Action);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo validar Google reCAPTCHA.");
            return false;
        }
    }

    private sealed class RecaptchaVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public decimal? Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
