using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public HomeController(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        if (SessionAuthHelper.IsOperatorAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Passenger");
        }

        if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Passenger");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (SessionAuthHelper.IsOperatorAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Passenger");
        }

        if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Passenger");
        }

        PopulateRecaptchaViewBag();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            PopulateRecaptchaViewBag();
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("Auth/login", request);
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                HttpContext.Session.SetString("Token", response.Token);
                HttpContext.Session.SetString("Rol", response.Rol);
                HttpContext.Session.SetString("Username", response.Username);
                HttpContext.Session.SetString("Nombres", response.Nombres);
                HttpContext.Session.SetString("Apellidos", response.Apellidos);

                if (string.Equals(response.Rol, "Pasajero", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Passenger");
                }

                return RedirectToAction("Index", "Passenger");
            }

            ModelState.AddModelError("", "Respuesta invÃ¡lida del servidor.");
            PopulateRecaptchaViewBag();
            return View(request);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al iniciar sesiÃ³n: {ex.Message}");
            PopulateRecaptchaViewBag();
            return View(request);
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void PopulateRecaptchaViewBag()
    {
        ViewBag.RecaptchaEnabled = _configuration.GetValue<bool>("Recaptcha:Enabled") &&
            !string.IsNullOrWhiteSpace(_configuration["Recaptcha:SiteKey"]);
        ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"] ?? string.Empty;
        ViewBag.RecaptchaAction = _configuration["Recaptcha:LoginAction"] ?? "login";
        ViewBag.RecaptchaVersion = _configuration["Recaptcha:Version"] ?? "v2";
    }
}

